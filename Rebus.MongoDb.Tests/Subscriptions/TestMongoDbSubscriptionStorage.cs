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
        MongoDatabase _mongoDatabase;

        public ISubscriptionStorage Create()
        {
            _mongoDatabase = MongoTestHelper.GetMongoDatabase();
            
            return new MongoDbSubscriptionStorage(_mongoDatabase, "subscriptions", true);
        }

        public void Cleanup()
        {
            _mongoDatabase.Drop();
            _mongoDatabase = null;
        }
    }
}