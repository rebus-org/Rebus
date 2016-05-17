using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Rebus.Auditing.Sagas;
using Rebus.Logging;
using Rebus.Persistence.SqlServer;
using Rebus.Sagas;
using Rebus.Serialization;
using Rebus.Tests.Contracts.Sagas;

namespace Rebus.Tests.Persistence.SqlServer
{
    public class SqlServerSnapshotStorageFactory : ISagaSnapshotStorageFactory
    {
        const string TableName = "SagaSnapshots";

        public SqlServerSnapshotStorageFactory()
        {
            SqlTestHelper.DropTable(TableName);
        }

        public ISagaSnapshotStorage Create()
        {
            var consoleLoggerFactory = new ConsoleLoggerFactory(true);
            var connectionProvider = new DbConnectionProvider(SqlTestHelper.ConnectionString, consoleLoggerFactory);

            var snapperino = new SqlServerSagaSnapshotStorage(connectionProvider, TableName, consoleLoggerFactory);

            snapperino.EnsureTableIsCreated();

            return snapperino;
        }

        public IEnumerable<SagaDataSnapshot> GetAllSnapshots()
        {
            return LoadStoredCopies(new DbConnectionProvider(SqlTestHelper.ConnectionString, new ConsoleLoggerFactory(true)), TableName).Result;
        }

        static async Task<List<SagaDataSnapshot>> LoadStoredCopies(DbConnectionProvider connectionProvider, string tableName)
        {
            var storedCopies = new List<SagaDataSnapshot>();

            using (var connection = await connectionProvider.GetConnection())
            {
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = $@"SELECT * FROM [{tableName}]";

                    using (var reader = command.ExecuteReader())
                    {
                        while (await reader.ReadAsync())
                        {
                            var sagaData = (ISagaData)new ObjectSerializer().Deserialize(Encoding.UTF8.GetBytes((string)reader["data"]));
                            var metadata = JsonConvert.DeserializeObject<Dictionary<string, string>>((string)reader["metadata"]);

                            storedCopies.Add(new SagaDataSnapshot{SagaData = sagaData, Metadata = metadata});
                        }
                    }
                }

                await connection.Complete();
            }
            return storedCopies;
        }
    
    }
}