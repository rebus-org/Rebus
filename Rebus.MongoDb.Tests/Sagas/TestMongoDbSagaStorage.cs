using MongoDB.Driver;
using Rebus.MongoDb.Sagas;
using Rebus.Sagas;
using Rebus.Tests.Contracts.Sagas;

namespace Rebus.MongoDb.Tests.Sagas
{
    public class TestMongoDbSagaStorage : ISagaStorageFactory
    {
        IMongoDatabase _mongoDatabase;
        IMongoClient _mongoClient;

        public ISagaStorage GetSagaStorage()
        {
            _mongoClient = MongoTestHelper.GetMongoClient();
            _mongoDatabase = MongoTestHelper.GetMongoDatabase(_mongoClient);

            return new MongoDbSagaStorage(_mongoDatabase);
        }

        public void CleanUp()
        {
            _mongoClient.DropDatabaseAsync(_mongoDatabase.DatabaseNamespace.DatabaseName).Wait();
            _mongoClient = null;
            _mongoDatabase = null;
        }
    }
}
