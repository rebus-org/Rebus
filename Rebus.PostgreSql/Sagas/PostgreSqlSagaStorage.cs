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
        static readonly string IdPropertyName = Reflect.Path<ISagaData>(d => d.Id);

        readonly ObjectSerializer _objectSerializer = new ObjectSerializer();
        readonly PostgresConnectionHelper _connectionHelper;
        readonly string _dataTableName;
        readonly string _indexTableName;
        readonly ILog _log;

        /// <summary>
        /// Constructs the saga storage
        /// </summary>
        public PostgreSqlSagaStorage(PostgresConnectionHelper connectionHelper, string dataTableName, string indexTableName, IRebusLoggerFactory rebusLoggerFactory)
        {
            if (connectionHelper == null) throw new ArgumentNullException(nameof(connectionHelper));
            if (dataTableName == null) throw new ArgumentNullException(nameof(dataTableName));
            if (indexTableName == null) throw new ArgumentNullException(nameof(indexTableName));
            if (rebusLoggerFactory == null) throw new ArgumentNullException(nameof(rebusLoggerFactory));
            _connectionHelper = connectionHelper;
            _dataTableName = dataTableName;
            _indexTableName = indexTableName;
            _log = rebusLoggerFactory.GetCurrentClassLogger();
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
                        $"The saga index table '{_indexTableName}' does not exist, so the automatic saga schema generation tried to run - but there was already a table named '{_dataTableName}', which was supposed to be created as the data table");
                }

                if (hasIndexTable)
                {
                    throw new ApplicationException(
                        $"The saga data table '{_dataTableName}' does not exist, so the automatic saga schema generation tried to run - but there was already a table named '{_indexTableName}', which was supposed to be created as the index table");
                }

                _log.Info("Saga tables '{0}' (data) and '{1}' (index) do not exist - they will be created now", _dataTableName, _indexTableName);

                using (var command = connection.CreateCommand())
                {
                    command.CommandText =
                        $@"
CREATE TABLE ""{_dataTableName}"" (
	""id"" UUID NOT NULL,
	""revision"" INTEGER NOT NULL,
	""data"" BYTEA NOT NULL,
	PRIMARY KEY (""id"")
);
";

                    command.ExecuteNonQuery();
                }

                using (var command = connection.CreateCommand())
                {
                    command.CommandText = $@"
CREATE TABLE ""{_indexTableName}"" (
	""saga_type"" TEXT NOT NULL,
	""key"" TEXT NOT NULL,
	""value"" TEXT NOT NULL,
	""saga_id"" UUID NOT NULL,
	PRIMARY KEY (""key"", ""value"", ""saga_type"")
);

CREATE INDEX ON ""{_indexTableName}"" (""saga_id"");
";

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
                        command.CommandText = $@"
SELECT s.""data"" 
    FROM ""{_dataTableName}"" s 
    WHERE s.""id"" = @id
";
                        command.Parameters.Add("id", NpgsqlDbType.Uuid).Value = ToGuid(propertyValue);
                    }
                    else
                    {
                        command.CommandText =
                            $@"
SELECT s.""data"" 
    FROM ""{_dataTableName}"" s
    JOIN ""{_indexTableName}"" i on s.id = i.saga_id 
    WHERE i.""saga_type"" = @saga_type AND i.""key"" = @key AND i.value = @value;
";

                        command.Parameters.Add("key", NpgsqlDbType.Text).Value = propertyName;
                        command.Parameters.Add("saga_type", NpgsqlDbType.Text).Value = GetSagaTypeName(sagaDataType);
                        command.Parameters.Add("value", NpgsqlDbType.Text).Value = (propertyValue ?? "").ToString();
                    }

                    var data = (byte[]) command.ExecuteScalar();

                    if (data == null) return null;

                    try
                    {
                        var sagaData = (ISagaData) _objectSerializer.Deserialize(data);

                        if (!sagaDataType.IsInstanceOfType(sagaData))
                        {
                            return null;
                        }

                        return sagaData;
                    }
                    catch (Exception exception)
                    {
                        var message =
                            $"An error occurred while attempting to deserialize '{data}' into a {sagaDataType}";

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
                throw new InvalidOperationException($"Saga data {sagaData.GetType()} has an uninitialized Id property!");
            }

            if (sagaData.Revision != 0)
            {
                throw new InvalidOperationException($"Attempted to insert saga data with ID {sagaData.Id} and revision {sagaData.Revision}, but revision must be 0 on first insert!");
            }

            using (var connection = await _connectionHelper.GetConnection())
            {
                using (var command = connection.CreateCommand())
                {
                    command.Parameters.Add("id", NpgsqlDbType.Uuid).Value = sagaData.Id;
                    command.Parameters.Add("revision", NpgsqlDbType.Integer).Value = sagaData.Revision;
                    command.Parameters.Add("data", NpgsqlDbType.Bytea).Value = _objectSerializer.Serialize(sagaData);

                    command.CommandText =
                        $@"

INSERT 
    INTO ""{_dataTableName}"" (""id"", ""revision"", ""data"") 
    VALUES (@id, @revision, @data);

";

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
                    command.CommandText = $@"

DELETE FROM ""{_indexTableName}"" WHERE ""saga_id"" = @id;

";
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

                    command.CommandText =
                        $@"

UPDATE ""{_dataTableName}"" 
    SET ""data"" = @data, ""revision"" = @next_revision 
    WHERE ""id"" = @id AND ""revision"" = @current_revision;

";
                    
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
                    command.CommandText =
                        $@"

DELETE 
    FROM ""{_dataTableName}"" 
    WHERE ""id"" = @id AND ""revision"" = @current_revision;

";
                    
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
                    command.CommandText =
                        $@"

DELETE 
    FROM ""{_indexTableName}"" 
    WHERE ""saga_id"" = @id

";
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
                    PropertyNameParameter = $"@n{i}",
                    PropertyValueParameter = $"@v{i}"
                })
                .ToList();

            // lastly, generate new index
            using (var command = connection.CreateCommand())
            {
                // generate batch insert with SQL for each entry in the index
                var inserts = parameters
                    .Select(a =>
                        $@"

INSERT
    INTO ""{_indexTableName}"" (""saga_type"", ""key"", ""value"", ""saga_id"") 
    VALUES (@saga_type, {a.PropertyNameParameter}, {a.PropertyValueParameter}, @saga_id)

");

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

                    return new KeyValuePair<string, string>(path, value?.ToString());
                })
                .Where(kvp => kvp.Value != null)
                .ToList();
        }
    }
}