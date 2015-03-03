using MongoDB.Bson;
using MongoDB.Driver;
using NUnit.Framework;
using Rebus.Tests.Contracts.Sagas;
using Rebus2.Sagas;

namespace Rebus.MongoDb.Tests
{
    [TestFixture]
    public class TestMongoDbSagaStorage : ISagaStorageFactory
    {
        public class BasicOperations : BasicOperations<TestMongoDbSagaStorage> { }

        public class ConcurrencyHandling : ConcurrencyHandling<TestMongoDbSagaStorage> { }

        public ISagaStorage GetSagaStorage()
        {
            var url = new MongoUrl("mongodb://localhost/rebus2_test");
            var settings = new MongoDatabaseSettings
            {
                GuidRepresentation = GuidRepresentation.Standard,
                WriteConcern = WriteConcern.Acknowledged
            };
            var mongoDatabase = new MongoClient(url).GetServer().GetDatabase(url.DatabaseName, settings);
            return new MongoDbSagaStorage(mongoDatabase);
        }

        public void Cleanup()
        {
            
        }
    }
}
