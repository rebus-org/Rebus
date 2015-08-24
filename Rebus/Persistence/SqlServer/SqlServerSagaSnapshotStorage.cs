using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Rebus.Auditing.Sagas;
using Rebus.Logging;
using Rebus.Sagas;

namespace Rebus.Persistence.SqlServer
{
    /// <summary>
    /// Implementation of <see cref="ISagaSnapshotStorage"/> that uses a table in SQL Server to store saga snapshots
    /// </summary>
    public class SqlServerSagaSnapshotStorage : ISagaSnapshotStorage
    {
        static ILog _log;

        static SqlServerSagaSnapshotStorage()
        {
            RebusLoggerFactory.Changed += f => _log = f.GetCurrentClassLogger();
        }

        readonly IDbConnectionProvider _connectionProvider;
        readonly string _tableName;

        static readonly JsonSerializerSettings DataSettings =
            new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.All };

        static readonly JsonSerializerSettings MetadataSettings =
            new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.None };

        /// <summary>
        /// Constructs the snapshot storage
        /// </summary>
        public SqlServerSagaSnapshotStorage(IDbConnectionProvider connectionProvider, string tableName)
        {
            _connectionProvider = connectionProvider;
            _tableName = tableName;
        }

        /// <summary>
        /// Creates the subscriptions table if necessary
        /// </summary>
        public void EnsureTableIsCreated()
        {
            using (var connection = _connectionProvider.GetConnection().Result)
            {
                var tableNames = connection.GetTableNames();

                if (tableNames.Contains(_tableName, StringComparer.OrdinalIgnoreCase))
                {
                    return;
                }

                _log.Info("Table '{0}' does not exist - it will be created now", _tableName);

                using (var command = connection.CreateCommand())
                {
                    command.CommandText = string.Format(@"
CREATE TABLE [dbo].[{0}] (
	[id] [uniqueidentifier] NOT NULL,
	[revision] [int] NOT NULL,
	[data] [nvarchar](max) NOT NULL,
	[metadata] [nvarchar](max) NOT NULL,
    CONSTRAINT [PK_{0}] PRIMARY KEY CLUSTERED 
    (
	    [id] ASC,
        [revision] ASC
    )
)
", _tableName);
                    command.ExecuteNonQuery();
                }

                connection.Complete();
            }
        }

        public async Task Save(ISagaData sagaData, Dictionary<string, string> sagaAuditMetadata)
        {
            using (var connection = await _connectionProvider.GetConnection())
            {
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = string.Format(@"

INSERT INTO [{0}] (
    [id],
    [revision],
    [data],
    [metadata]
) VALUES (
    @id, 
    @revision, 
    @data,
    @metadata
)

", _tableName);
                    command.Parameters.Add("id", SqlDbType.UniqueIdentifier).Value = sagaData.Id;
                    command.Parameters.Add("revision", SqlDbType.Int).Value = sagaData.Revision;
                    command.Parameters.Add("data", SqlDbType.NVarChar).Value = JsonConvert.SerializeObject(sagaData, DataSettings);
                    command.Parameters.Add("metadata", SqlDbType.NVarChar).Value = JsonConvert.SerializeObject(sagaAuditMetadata, MetadataSettings);

                    await command.ExecuteNonQueryAsync();
                }

                await connection.Complete();
            }
        }
    }
}