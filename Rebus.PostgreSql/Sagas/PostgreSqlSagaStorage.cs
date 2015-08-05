using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Npgsql;
using NpgsqlTypes;
using Rebus.Exceptions;
using Rebus.Extensions;
using Rebus.Logging;
using Rebus.PostgreSql.Reflection;
using Rebus.Sagas;
using Rebus.Serialization;

namespace Rebus.PostgreSql.Sagas
{
    /// <summary>
    /// Implementation of <see cref="ISagaStorage"/> that uses PostgreSQL to do its thing
    /// </summary>
    public class PostgreSqlSagaStorage : ISagaStorage
    {
        const bool IndexNullProperties = false;

        static ILog _log;

        static PostgreSqlSagaStorage()
        {
            RebusLoggerFactory.Changed += f => _log = f.GetCurrentClassLogger();
        }

        static readonly string IdPropertyName = Reflect.Path<ISagaData>(d => d.Id);

        readonly ObjectSerializer _objectSerializer = new ObjectSerializer();
        readonly PostgresConnectionHelper _connectionHelper;
        readonly string _dataTableName;
        readonly string _indexTableName;

        /// <summary>
        /// Constructs the saga storage
        /// </summary>
        public PostgreSqlSagaStorage(PostgresConnectionHelper connectionHelper, string dataTableName, string indexTableName)
        {
            _connectionHelper = connectionHelper;
            _dataTableName = dataTableName;
            _indexTableName = indexTableName;
        }

        /// <summary>
        /// Checks to see if the configured saga data and saga index table exists. If they both exist, we'll continue, if
        /// neigther of them exists, we'll try to create them. If one of them exists, we'll throw an error.
        /// </summary>
        public void EnsureTablesAreCreated()
        {
            using (var connection = _connectionHelper.GetConnection().Result)
            {
                var tableNames = connection.GetTableNames().ToHashSet();

                var hasDataTable = tableNames.Contains(_dataTableName);
                var hasIndexTable = tableNames.Contains(_indexTableName);

                if (hasDataTable && hasIndexTable)
                {
                    return;
                }

                if (hasDataTable)
                {
                    throw new ApplicationException(
                        string.Format(
                            "The saga index table '{0}' does not exist, so the automatic saga schema generation tried to run - but there was already a table named '{1}', which was supposed to be created as the data table",
                            _indexTableName, _dataTableName));
                }

                if (hasIndexTable)
                {
                    throw new ApplicationException(
                        string.Format(
                            "The saga data table '{0}' does not exist, so the automatic saga schema generation tried to run - but there was already a table named '{1}', which was supposed to be created as the index table",
                            _dataTableName, _indexTableName));
                }

                _log.Info("Saga tables '{0}' (data) and '{1}' (index) do not exist - they will be created now", _dataTableName, _indexTableName);

                using (var command = connection.CreateCommand())
                {
                    command.CommandText = string.Format(@"
CREATE TABLE ""{0}"" (
	""id"" UUID NOT NULL,
	""revision"" INTEGER NOT NULL,
	""data"" BYTEA NOT NULL,
	PRIMARY KEY (""id"")
);
", _dataTableName);

                    command.ExecuteNonQuery();
                }

                using (var command = connection.CreateCommand())
                {
                    command.CommandText = string.Format(@"
CREATE TABLE ""{0}"" (
	""saga_type"" TEXT NOT NULL,
	""key"" TEXT NOT NULL,
	""value"" TEXT NOT NULL,
	""saga_id"" UUID NOT NULL,
	PRIMARY KEY (""key"", ""value"", ""saga_type"")
);

CREATE INDEX ON ""{0}"" (""saga_id"");

", _indexTableName);

                    command.ExecuteNonQuery();
                }

                connection.Complete();
            }
        }

        public async Task<ISagaData> Find(Type sagaDataType, string propertyName, object propertyValue)
        {
            using (var connection = await _connectionHelper.GetConnection())
            {
                using (var command = connection.CreateCommand())
                {
                    if (propertyName == IdPropertyName)
                    {
                        const string sql = @"SELECT s.data FROM ""{0}"" s WHERE s.id = @id";

                        command.CommandText = string.Format(sql, _dataTableName);
                        command.Parameters.Add("id", NpgsqlDbType.Uuid).Value = ToGuid(propertyValue);
                    }
                    else
                    {
                        const string sql = @"
SELECT s.data 
    FROM ""{0}"" s
    JOIN ""{1}"" i on s.id = i.saga_id 
    WHERE i.""saga_type"" = @saga_type AND i.""key"" = @key AND i.value = @value
";

                        command.CommandText = string.Format(sql, _dataTableName, _indexTableName);

                        command.Parameters.Add("key", NpgsqlDbType.Text).Value = propertyName;
                        command.Parameters.Add("saga_type", NpgsqlDbType.Text).Value = GetSagaTypeName(sagaDataType);
                        command.Parameters.Add("value", NpgsqlDbType.Text).Value = (propertyValue ?? "").ToString();
                    }

                    var data = (byte[]) command.ExecuteScalar();

                    if (data == null) return null;

                    try
                    {
                        return (ISagaData) _objectSerializer.Deserialize(data);
                    }
                    catch (Exception exception)
                    {
                        var message = string.Format(
                            "An error occurred while attempting to deserialize '{0}' into a {1}", data,
                            sagaDataType);

                        throw new ApplicationException(message, exception);
                    }
                    finally
                    {
                        connection.Complete();
                    }
                }
            }
        }

        static object ToGuid(object propertyValue)
        {
            return Convert.ChangeType(propertyValue, typeof(Guid));
        }

        public async Task Insert(ISagaData sagaData, IEnumerable<ISagaCorrelationProperty> correlationProperties)
        {
            if (sagaData.Id == Guid.Empty)
            {
                throw new InvalidOperationException(string.Format("Saga data {0} has an uninitialized Id property!", sagaData.GetType()));
            }

            using (var connection = await _connectionHelper.GetConnection())
            {
                using (var command = connection.CreateCommand())
                {
                    command.Parameters.Add("id", NpgsqlDbType.Uuid).Value = sagaData.Id;
                    command.Parameters.Add("revision", NpgsqlDbType.Integer).Value = sagaData.Revision;
                    command.Parameters.Add("data", NpgsqlDbType.Bytea).Value = _objectSerializer.Serialize(sagaData);

                    command.CommandText = string.Format(@"

INSERT 
    INTO ""{0}"" (""id"", ""revision"", ""data"") 
    VALUES (@id, @revision, @data);

", _dataTableName);

                    try
                    {
                        command.ExecuteNonQuery();
                    }
                    catch (NpgsqlException exception)
                    {
                        throw new ConcurrencyException(exception, "Saga data {0} with ID {1} in table {2} could not be inserted!",
                            sagaData.GetType(), sagaData.Id, _dataTableName);
                    }
                }

                var propertiesToIndex = GetPropertiesToIndex(sagaData, correlationProperties);

                if (propertiesToIndex.Any())
                {
                    await CreateIndex(sagaData, connection, propertiesToIndex);
                }

                connection.Complete();
            }
        }

        public async Task Update(ISagaData sagaData, IEnumerable<ISagaCorrelationProperty> correlationProperties)
        {
            using (var connection = await _connectionHelper.GetConnection())
            {
                var revisionToUpdate = sagaData.Revision;

                sagaData.Revision++;
                
                var nextRevision = sagaData.Revision;

                // first, delete existing index
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = string.Format(@"

DELETE FROM ""{0}"" WHERE ""saga_id"" = @id;

", _indexTableName);
                    command.Parameters.Add("id", NpgsqlDbType.Uuid).Value = sagaData.Id;
                    await command.ExecuteNonQueryAsync();
                }

                // next, update or insert the saga
                using (var command = connection.CreateCommand())
                {
                    command.Parameters.Add("id", NpgsqlDbType.Uuid).Value = sagaData.Id;
                    command.Parameters.Add("current_revision", NpgsqlDbType.Integer).Value = revisionToUpdate;
                    command.Parameters.Add("next_revision", NpgsqlDbType.Integer).Value = nextRevision;
                    command.Parameters.Add("data", NpgsqlDbType.Bytea).Value = _objectSerializer.Serialize(sagaData);

                    command.CommandText = string.Format(@"

UPDATE ""{0}"" 
    SET ""data"" = @data, ""revision"" = @next_revision 
    WHERE ""id"" = @id AND ""revision"" = @current_revision;

", _dataTableName);
                    
                    var rows = await command.ExecuteNonQueryAsync();
                    
                    if (rows == 0)
                    {
                        throw new ConcurrencyException("Update of saga with ID {0} did not succeed because someone else beat us to it", sagaData.Id);
                    }
                }

                var propertiesToIndex = GetPropertiesToIndex(sagaData, correlationProperties);

                if (propertiesToIndex.Any())
                {
                    await CreateIndex(sagaData, connection, propertiesToIndex);
                }

                connection.Complete();
            }
        }

        public async Task Delete(ISagaData sagaData)
        {
            using (var connection = await _connectionHelper.GetConnection())
            {
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = string.Format(@"

DELETE 
    FROM ""{0}"" 
    WHERE ""id"" = @id AND ""revision"" = @current_revision;

", _dataTableName);
                    
                    command.Parameters.Add("id", NpgsqlDbType.Uuid).Value = sagaData.Id;
                    command.Parameters.Add("current_revision", NpgsqlDbType.Integer).Value = sagaData.Revision;

                    var rows = await command.ExecuteNonQueryAsync();

                    if (rows == 0)
                    {
                        throw new ConcurrencyException("Delete of saga with ID {0} did not succeed because someone else beat us to it", sagaData.Id);
                    }
                }

                using (var command = connection.CreateCommand())
                {
                    command.CommandText = string.Format(@"

DELETE 
    FROM ""{0}"" 
    WHERE ""saga_id"" = @id

", _indexTableName);
                    command.Parameters.Add("id", NpgsqlDbType.Uuid).Value = sagaData.Id;
                    
                    await command.ExecuteNonQueryAsync();
                }

                connection.Complete();
            }
        }

        async Task CreateIndex(ISagaData sagaData, PostgresConnection connection, IEnumerable<KeyValuePair<string, string>> propertiesToIndex)
        {
            var sagaTypeName = GetSagaTypeName(sagaData.GetType());
            var parameters = propertiesToIndex
                .Select((p, i) => new
                {
                    PropertyName = p.Key,
                    PropertyValue = p.Value ?? "",
                    PropertyNameParameter = string.Format("@n{0}", i),
                    PropertyValueParameter = string.Format("@v{0}", i)
                })
                .ToList();

            // lastly, generate new index
            using (var command = connection.CreateCommand())
            {
                // generate batch insert with SQL for each entry in the index
                var inserts = parameters
                    .Select(a => string.Format(@"

INSERT
    INTO ""{0}"" (""saga_type"", ""key"", ""value"", ""saga_id"") 
    VALUES (@saga_type, {1}, {2}, @saga_id)

",
                        _indexTableName, a.PropertyNameParameter, a.PropertyValueParameter));

                var sql = string.Join(";" + Environment.NewLine, inserts);

                command.CommandText = sql;

                foreach (var parameter in parameters)
                {
                    command.Parameters.Add(parameter.PropertyNameParameter, NpgsqlDbType.Text).Value = parameter.PropertyName;
                    command.Parameters.Add(parameter.PropertyValueParameter, NpgsqlDbType.Text).Value = parameter.PropertyValue;
                }

                command.Parameters.Add("saga_type", NpgsqlDbType.Text).Value = sagaTypeName;
                command.Parameters.Add("saga_id", NpgsqlDbType.Uuid).Value = sagaData.Id;

                await command.ExecuteNonQueryAsync();
            }
        }

        string GetSagaTypeName(Type sagaDataType)
        {
            return sagaDataType.FullName;
        }

        static List<KeyValuePair<string, string>> GetPropertiesToIndex(ISagaData sagaData, IEnumerable<ISagaCorrelationProperty> correlationProperties)
        {
            return correlationProperties
                .Select(p => p.PropertyName)
                .Select(path =>
                {
                    var value = Reflect.Value(sagaData, path);

                    return new KeyValuePair<string, string>(path, value != null ? value.ToString() : null);
                })
                .Where(kvp => IndexNullProperties || kvp.Value != null)
                .ToList();
        }
    }
}