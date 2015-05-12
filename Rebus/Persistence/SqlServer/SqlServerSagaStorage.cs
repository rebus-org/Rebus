using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Rebus.Exceptions;
using Rebus.Logging;
using Rebus.Reflection;
using Rebus.Sagas;

namespace Rebus.Persistence.SqlServer
{
    /// <summary>
    /// Implementation of <see cref="ISagaStorage"/> that persists saga data as a Newtonsoft JSON.NET-serialized object to a table in SQL Server.
    /// Correlation properties are stored in a separate index table, allowing for looking up saga data instanes based on the configured correlation
    /// properties
    /// </summary>
    public class SqlServerSagaStorage : ISagaStorage
    {
        const int MaximumSagaDataTypeNameLength = 40; 
        
        static ILog _log;

        static SqlServerSagaStorage()
        {
            RebusLoggerFactory.Changed += f => _log = f.GetCurrentClassLogger();
        }

        static readonly JsonSerializerSettings Settings =
            new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.All };

        readonly DbConnectionProvider _connectionProvider;
        readonly string _dataTableName;
        readonly string _indexTableName;
        readonly string _idPropertyName = Reflect.Path<ISagaData>(d => d.Id);
        const bool IndexNullProperties = false;

        /// <summary>
        /// Constructs the saga storage, using the specified connection provider and tables for persistence.
        /// </summary>
        public SqlServerSagaStorage(DbConnectionProvider connectionProvider, string dataTableName, string indexTableName)
        {
            _connectionProvider = connectionProvider;
            _dataTableName = dataTableName;
            _indexTableName = indexTableName;
        }

        /// <summary>
        /// Checks to see if the configured tables exist, creating them if necessary
        /// </summary>
        public void EnsureTablesAreCreated()
        {
            using (var connection = _connectionProvider.GetConnection().Result)
            {
                var tableNames = connection.GetTableNames().ToList();

                if (tableNames.Contains(_dataTableName, StringComparer.OrdinalIgnoreCase))
                {
                    return;
                }

                if (tableNames.Contains(_indexTableName, StringComparer.OrdinalIgnoreCase))
                {
                    throw new ApplicationException(string.Format("The saga data table '{0}' does not exist, so the automatic saga schema generation tried to run - but there was already a table named '{1}', which was supposed to be created as the index table",
                        _dataTableName, _indexTableName));
                }

                _log.Info("Saga tables '{0}' (data) and '{1}' (index) do not exist - they will be created now", _dataTableName, _indexTableName);

                using (var command = connection.CreateCommand())
                {
                    command.CommandText = string.Format(@"
CREATE TABLE [dbo].[{0}] (
	[id] [uniqueidentifier] NOT NULL,
	[revision] [int] NOT NULL,
	[data] [nvarchar](max) NOT NULL,
    CONSTRAINT [PK_{0}] PRIMARY KEY CLUSTERED 
    (
	    [id] ASC
    )
)
", _dataTableName);
                    
                    command.ExecuteNonQuery();
                }

                using (var command = connection.CreateCommand())
                {
                    command.CommandText = string.Format(@"
CREATE TABLE [dbo].[{0}] (
	[saga_type] [nvarchar](40) NOT NULL,
	[key] [nvarchar](200) NOT NULL,
	[value] [nvarchar](200) NOT NULL,
	[saga_id] [uniqueidentifier] NOT NULL,
    CONSTRAINT [PK_{0}] PRIMARY KEY CLUSTERED 
    (
	    [key] ASC,
	    [value] ASC,
	    [saga_type] ASC
    )
)

CREATE NONCLUSTERED INDEX [IX_{0}_saga_id] ON [dbo].[{0}]
(
	[saga_id] ASC
)
", _indexTableName);
                    
                    command.ExecuteNonQuery();
                }

                using (var command = connection.CreateCommand())
                {
                    command.CommandText = string.Format(@"
ALTER TABLE [dbo].[{0}] WITH CHECK 
    ADD CONSTRAINT [FK_{1}_id] FOREIGN KEY([saga_id])

REFERENCES [dbo].[{1}] ([id]) ON DELETE CASCADE
", _indexTableName, _dataTableName);
                    
                    command.ExecuteNonQuery();
                }

                using (var command = connection.CreateCommand())
                {
                    command.CommandText = string.Format(@"
ALTER TABLE [dbo].[{0}] CHECK CONSTRAINT [FK_{1}_id]
", _indexTableName, _dataTableName);

                    command.ExecuteNonQuery();
                }

                connection.Complete().Wait();
            }
        }

        public async Task<ISagaData> Find(Type sagaDataType, string propertyName, object propertyValue)
        {
            using (var connection = await _connectionProvider.GetConnection())
            {
                using (var command = connection.CreateCommand())
                {
                    if (propertyName == _idPropertyName)
                    {
                        command.CommandText = string.Format(@"SELECT [data] FROM [{0}] WHERE [id] = @value", _dataTableName);
                    }
                    else
                    {
                        command.CommandText = string.Format(@"
SELECT [saga].[data] FROM [{0}] [saga] 
    JOIN [{1}] [index] ON [saga].[id] = [index].[saga_id] 
WHERE [index].[saga_type] = @saga_type
    AND [index].[key] = @key 
    AND [index].[value] = @value", _dataTableName, _indexTableName);

                        command.Parameters.AddWithValue("key", propertyName);
                        command.Parameters.AddWithValue("saga_type", GetSagaTypeName(sagaDataType));
                    }

                    command.Parameters.AddWithValue("value", (propertyValue ?? "").ToString());

                    var value = (string)command.ExecuteScalar();

                    if (value == null) return null;

                    try
                    {
                        return (ISagaData)JsonConvert.DeserializeObject(value, Settings);
                    }
                    catch (Exception exception)
                    {
                        throw new ApplicationException(
                            string.Format("An error occurred while attempting to deserialize '{0}' into a {1}",
                                value, sagaDataType), exception);
                    }
                }
            }
        }

        public async Task Insert(ISagaData sagaData, IEnumerable<ISagaCorrelationProperty> correlationProperties)
        {
            if (sagaData.Id == Guid.Empty)
            {
                throw new InvalidOperationException(string.Format("Saga data {0} has an uninitialized Id property!", sagaData.GetType()));
            }

            using (var connection = await _connectionProvider.GetConnection())
            {
                using (var command = connection.CreateCommand())
                {
                    command.Parameters.AddWithValue("id", sagaData.Id);
                    command.Parameters.AddWithValue("revision", sagaData.Revision);
                    command.Parameters.AddWithValue("data", JsonConvert.SerializeObject(sagaData, Formatting.Indented, Settings));

                    command.CommandText = string.Format(@"INSERT INTO [{0}] ([id], [revision], [data]) VALUES (@id, @revision, @data)", _dataTableName);
                    try
                    {
                        command.ExecuteNonQuery();
                    }
                    catch (SqlException sqlException)
                    {
                        if (sqlException.Number == SqlServerMagic.PrimaryKeyViolationNumber)
                        {
                            throw new ConcurrencyException("An exception occurred while attempting to insert saga data with ID {0}", sagaData.Id);
                        }

                        throw;
                    }
                }

                var propertiesToIndex = GetPropertiesToIndex(sagaData, correlationProperties);

                if (propertiesToIndex.Any())
                {
                    CreateIndex(propertiesToIndex, connection, sagaData);
                }

                await connection.Complete();
            }
        }

        public async Task Update(ISagaData sagaData, IEnumerable<ISagaCorrelationProperty> correlationProperties)
        {
            using (var connection = await _connectionProvider.GetConnection())
            {
                var revisionToUpdate = sagaData.Revision;
                sagaData.Revision++;

                try
                {
                    // first, delete existing index
                    using (var command = connection.CreateCommand())
                    {
                        command.CommandText = string.Format(@"DELETE FROM [{0}] WHERE [saga_id] = @id", _indexTableName);
                        command.Parameters.AddWithValue("id", sagaData.Id);
                        command.ExecuteNonQuery();
                    }

                    // next, update or insert the saga
                    using (var command = connection.CreateCommand())
                    {
                        command.Parameters.AddWithValue("id", sagaData.Id);
                        command.Parameters.AddWithValue("current_revision", revisionToUpdate);
                        command.Parameters.AddWithValue("next_revision", sagaData.Revision);
                        command.Parameters.AddWithValue("data", JsonConvert.SerializeObject(sagaData, Formatting.Indented, Settings));

                        command.CommandText = string.Format(@"
UPDATE [{0}] 
    SET [data] = @data, [revision] = @next_revision 
    WHERE [id] = @id AND [revision] = @current_revision", _dataTableName);

                        var rows = command.ExecuteNonQuery();

                        if (rows == 0)
                        {
                            throw new ConcurrencyException("Update of saga with ID {0} did not succeed because someone else beat us to it", sagaData.Id);
                        }
                    }

                    var propertiesToIndex = GetPropertiesToIndex(sagaData, correlationProperties);

                    if (propertiesToIndex.Any())
                    {
                        CreateIndex(propertiesToIndex, connection, sagaData);
                    }

                    await connection.Complete();
                }
                catch
                {
                    sagaData.Revision--;
                    throw;
                }
            }
        }

        public async Task Delete(ISagaData sagaData)
        {
            using (var connection = await _connectionProvider.GetConnection())
            {
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = string.Format(@"DELETE FROM [{0}] WHERE [id] = @id AND [revision] = @current_revision;", _dataTableName);
                    command.Parameters.AddWithValue("id", sagaData.Id);
                    command.Parameters.AddWithValue("current_revision", sagaData.Revision);
                    var rows = command.ExecuteNonQuery();
                    if (rows == 0)
                    {
                        throw new ConcurrencyException("Delete of saga with ID {0} did not succeed because someone else beat us to it", sagaData.Id);
                    }
                }

                using (var command = connection.CreateCommand())
                {
                    command.CommandText = string.Format(@"DELETE FROM [{0}] WHERE [saga_id] = @id", _indexTableName);
                    command.Parameters.AddWithValue("id", sagaData.Id);
                    command.ExecuteNonQuery();
                }

                await connection.Complete();
            }
        }

        void CreateIndex(IEnumerable<KeyValuePair<string, string>> propertiesToIndex, IDbConnection connection, ISagaData sagaData)
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
                    .Select(a => string.Format(
                        @"
INSERT INTO [{0}]
    ([saga_type], [key], [value], [saga_id]) 
VALUES
    (@saga_type, {1}, {2}, @saga_id)
",
                        _indexTableName, a.PropertyNameParameter, a.PropertyValueParameter))
                    .ToList();

                var sql = string.Join(";" + Environment.NewLine, inserts);

                command.CommandText = sql;

                foreach (var parameter in parameters)
                {
                    command.Parameters.Add(parameter.PropertyNameParameter, SqlDbType.NVarChar).Value = parameter.PropertyName;
                    command.Parameters.Add(parameter.PropertyValueParameter, SqlDbType.NVarChar).Value = parameter.PropertyValue;
                }

                command.Parameters.Add("saga_type", SqlDbType.NVarChar).Value = sagaTypeName;
                command.Parameters.Add("saga_id", SqlDbType.UniqueIdentifier).Value = sagaData.Id;

                try
                {
                    command.ExecuteNonQuery();
                }
                catch (SqlException sqlException)
                {
                    if (sqlException.Number == SqlServerMagic.PrimaryKeyViolationNumber)
                    {
                        throw new ConcurrencyException("Could not update index for saga with ID {0}", sagaData.Id);
                    }

                    throw;
                }
            }

        }

        string GetSagaTypeName(Type sagaDataType)
        {
            var sagaTypeName = sagaDataType.Name;

            if (sagaTypeName.Length > MaximumSagaDataTypeNameLength)
            {
                throw new InvalidOperationException(
                    string.Format(
                        @"Sorry, but the maximum length of the name of a saga data class is currently limited to {0} characters!
This is due to a limitation in SQL Server, where compound indexes have a 900 byte upper size limit - and
since the saga index needs to be able to efficiently query by saga type, key, and value at the same time,
there's room for only 200 characters as the key, 200 characters as the value, and 40 characters as the
saga type name.",
                        MaximumSagaDataTypeNameLength));
            }

            return sagaTypeName;
        }

        List<KeyValuePair<string, string>> GetPropertiesToIndex(ISagaData sagaData, IEnumerable<ISagaCorrelationProperty> correlationProperties)
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