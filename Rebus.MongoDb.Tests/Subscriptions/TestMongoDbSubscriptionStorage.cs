using MongoDB.Driver;
using NUnit.Framework;
using Rebus.MongoDb.Subscriptions;
using Rebus.Subscriptions;
using Rebus.Tests.Contracts.Subscriptions;

namespace Rebus.MongoDb.Tests.Subscriptions
{
    [TestFixture]
    public class BasicSubscriptionOperations : BasicSubscriptionOperations<TestMongoDbSubscriptionStorage> { }

    public class TestMongoDbSubscriptionStorage : ISubscriptionStorageFactory
    {
        IMongoDatabase _mongoDatabase;
        IMongoClient _mongoClient;

        public ISubscriptionStorage Create()
        {
            _mongoClient = MongoTestHelper.GetMongoClient();
            _mongoDatabase = MongoTestHelper.GetMongoDatabase(_mongoClient);
            
            return new MongoDbSubscriptionStorage(_mongoDatabase, "subscriptions", true);
        }

        public void Cleanup()
        {
            _mongoClient.DropDatabaseAsync(_mongoDatabase.DatabaseNamespace.DatabaseName).Wait();
            _mongoClient = null;
            _mongoDatabase = null;
        }
    }
}