using MongoDB.Bson;
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
            _mongoDatabase = GetMongoDatabase();

            return new MongoDbSagaStorage(_mongoDatabase);
        }

        public void Cleanup()
        {
            _mongoDatabase.Drop();
            _mongoDatabase = null;
        }

        static MongoDatabase GetMongoDatabase()
        {
            var url = MongoTestHelper.GetUrl();
            var settings = new MongoDatabaseSettings
            {
                GuidRepresentation = GuidRepresentation.Standard,
                WriteConcern = WriteConcern.Acknowledged
            };
            var mongoDatabase = new MongoClient(url).GetServer().GetDatabase(url.DatabaseName, settings);
            return mongoDatabase;
        }
    }
}
