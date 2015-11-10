using System.Collections.Generic;
using Rebus.Auditing.Sagas;
using Rebus.PostgreSql.Sagas;
using Rebus.Sagas;
using Rebus.Serialization;
using Rebus.Tests.Contracts.Sagas;

namespace Rebus.PostgreSql.Tests.Sagas
{
    public class PostgreSqlSnapshotStorageFactory : ISagaSnapshotStorageFactory
    {
        const string TableName = "SagaSnaps";

        public PostgreSqlSnapshotStorageFactory()
        {
            PostgreSqlTestHelper.DropTable(TableName);
        }

        public ISagaSnapshotStorage Create()
        {
            var snapshotStorage = new PostgreSqlSagaSnapshotStorage(PostgreSqlTestHelper.ConnectionHelper, TableName);

            snapshotStorage.EnsureTableIsCreated();

            return snapshotStorage;
        }

        public IEnumerable<SagaDataSnapshot> GetAllSnapshots()
        {
            using (var connection = PostgreSqlTestHelper.ConnectionHelper.GetConnection().Result)
            {
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = string.Format(@"SELECT ""data"", ""metadata"" FROM ""{0}""", TableName);

                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var data = (byte[])reader["data"];
                            var metadataString = (string)reader["metadata"];

                            var objectSerializer = new ObjectSerializer();
                            var dictionarySerializer = new DictionarySerializer();

                            var sagaData = objectSerializer.Deserialize(data);
                            var metadata = dictionarySerializer.DeserializeFromString(metadataString);

                            yield return new SagaDataSnapshot
                            {
                                SagaData = (ISagaData) sagaData,
                                Metadata = metadata
                            };
                        }
                    }
                }
            }
        }
    }
}