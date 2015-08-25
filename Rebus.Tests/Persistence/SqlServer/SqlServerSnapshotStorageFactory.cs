using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Rebus.Auditing.Sagas;
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
            var connectionProvider = new DbConnectionProvider(SqlTestHelper.ConnectionString);

            var snapperino = new SqlServerSagaSnapshotStorage(connectionProvider, TableName);

            snapperino.EnsureTableIsCreated();

            return snapperino;
        }

        public IEnumerable<SagaDataSnapshot> GetAllSnapshots()
        {
            return LoadStoredCopies(new DbConnectionProvider(SqlTestHelper.ConnectionString), TableName).Result;
        }

        static async Task<List<SagaDataSnapshot>> LoadStoredCopies(DbConnectionProvider connectionProvider, string tableName)
        {
            var storedCopies = new List<SagaDataSnapshot>();

            using (var connection = await connectionProvider.GetConnection())
            {
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = string.Format(@"SELECT * FROM [{0}]", tableName);

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