using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NpgsqlTypes;
using Rebus.Auditing.Sagas;
using Rebus.Extensions;
using Rebus.Sagas;
using Rebus.Serialization;

namespace Rebus.PostgreSql.Sagas
{
    /// <summary>
    /// Implementation of <see cref="ISagaSnapshotStorage"/> that uses PostgreSQL to store the snapshots
    /// </summary>
    public class PostgreSqlSagaSnapshotStorage : ISagaSnapshotStorage
    {
        readonly ObjectSerializer _objectSerializer = new ObjectSerializer();
        readonly DictionarySerializer _dictionarySerializer = new DictionarySerializer();
        readonly PostgresConnectionHelper _connectionHelper;
        readonly string _tableName;

        /// <summary>
        /// Constructs the storage
        /// </summary>
        public PostgreSqlSagaSnapshotStorage(PostgresConnectionHelper connectionHelper, string tableName)
        {
            if (connectionHelper == null) throw new ArgumentNullException(nameof(connectionHelper));
            if (tableName == null) throw new ArgumentNullException(nameof(tableName));
            _connectionHelper = connectionHelper;
            _tableName = tableName;
        }

        public async Task Save(ISagaData sagaData, Dictionary<string, string> sagaAuditMetadata)
        {
            using (var connection = await _connectionHelper.GetConnection())
            {
                using (var command = connection.CreateCommand())
                {
                    command.CommandText =
                        $@"

INSERT
    INTO ""{_tableName}"" (""id"", ""revision"", ""data"", ""metadata"")
    VALUES (@id, @revision, @data, @metadata);

";
                    command.Parameters.Add("id", NpgsqlDbType.Uuid).Value = sagaData.Id;
                    command.Parameters.Add("revision", NpgsqlDbType.Integer).Value = sagaData.Revision;
                    command.Parameters.Add("data", NpgsqlDbType.Bytea).Value = _objectSerializer.Serialize(sagaData);
                    command.Parameters.Add("metadata", NpgsqlDbType.Jsonb).Value =
                        _dictionarySerializer.SerializeToString(sagaAuditMetadata);

                    await command.ExecuteNonQueryAsync();
                }
                
                connection.Complete();
            }
        }

        /// <summary>
        /// Creates the necessary table if it does not already exist
        /// </summary>
        public void EnsureTableIsCreated()
        {
            using (var connection = _connectionHelper.GetConnection().Result)
            {
                var tableNames = connection.GetTableNames().ToHashSet();

                if (tableNames.Contains(_tableName)) return;

                using (var command = connection.CreateCommand())
                {
                    command.CommandText =
                        $@"
CREATE TABLE ""{_tableName}"" (
	""id"" UUID NOT NULL,
	""revision"" INTEGER NOT NULL,
	""metadata"" JSONB NOT NULL,
	""data"" BYTEA NOT NULL,
	PRIMARY KEY (""id"", ""revision"")
);
";

                    command.ExecuteNonQuery();
                }

                 connection.Complete();
            }
        }
    }
}