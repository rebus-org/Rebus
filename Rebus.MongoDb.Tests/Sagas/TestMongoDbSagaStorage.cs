using MongoDB.Driver;
using Rebus.MongoDb.Sagas;
using Rebus.Sagas;
using Rebus.Tests.Contracts.Sagas;

namespace Rebus.MongoDb.Tests.Sagas
{
    public class TestMongoDbSagaStorage : ISagaStorageFactory
    {
        MongoDatabase _mongoDatabase;

        public ISagaStorage GetSagaStorage()
        {
            _mongoDatabase = MongoTestHelper.GetMongoDatabase();

            return new MongoDbSagaStorage(_mongoDatabase);
        }

        public void CleanUp()
        {
            _mongoDatabase.Drop();
            _mongoDatabase = null;
        }
    }
}
