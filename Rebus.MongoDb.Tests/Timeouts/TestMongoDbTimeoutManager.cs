using MongoDB.Driver;
using NUnit.Framework;
using Rebus.MongoDb.Timeouts;
using Rebus.Tests;
using Rebus.Tests.Contracts.Timeouts;
using Rebus.Timeouts;

namespace Rebus.MongoDb.Tests.Timeouts
{
    [TestFixture]
    public class TestMongoDbTimeoutManager : BasicStoreAndRetrieveOperations<MongoDbTimeoutManagerFactory>
    {
         
    }

    public class MongoDbTimeoutManagerFactory : ITimeoutManagerFactory
    {
        readonly MongoDatabase _mongoDatabase;
        readonly string _collectionName = string.Format("timeouts_{0}", TestConfig.Suffix);

        public MongoDbTimeoutManagerFactory()
        {
            _mongoDatabase = MongoTestHelper.GetMongoDatabase();
            DropCollection(_collectionName);
        }
        
        public ITimeoutManager Create()
        {
            return new MongoDbTimeoutManager(_mongoDatabase, _collectionName);
        }

        public void Cleanup()
        {
            DropCollection(_collectionName);
        }

        void DropCollection(string collectionName)
        {
            _mongoDatabase.GetCollection(collectionName).Drop();
        }
    }
}