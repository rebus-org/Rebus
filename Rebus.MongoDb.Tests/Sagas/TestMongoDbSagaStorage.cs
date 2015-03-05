using MongoDB.Driver;
using NUnit.Framework;
using Rebus.MongoDb.Sagas;
using Rebus.Sagas;
using Rebus.Tests.Contracts.Sagas;

namespace Rebus.MongoDb.Tests.Sagas
{
    [TestFixture]
    public class BasicLoadAndSaveAndFindOperations : BasicLoadAndSaveAndFindOperations<TestMongoDbSagaStorage> { }

    [TestFixture]
    public class ConcurrencyHandling : ConcurrencyHandling<TestMongoDbSagaStorage> { }

    public class TestMongoDbSagaStorage : ISagaStorageFactory
    {
        MongoDatabase _mongoDatabase;

        public ISagaStorage GetSagaStorage()
        {
            _mongoDatabase = MongoTestHelper.GetMongoDatabase();

            return new MongoDbSagaStorage(_mongoDatabase);
        }

        public void Cleanup()
        {
            _mongoDatabase.Drop();
            _mongoDatabase = null;
        }
    }
}
