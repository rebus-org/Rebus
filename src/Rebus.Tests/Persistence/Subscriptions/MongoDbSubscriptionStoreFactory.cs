using MongoDB.Driver;
using Rebus.MongoDb;

namespace Rebus.Tests.Persistence.Subscriptions
{
    public class MongoDbSubscriptionStoreFactory : ISubscriptionStoreFactory
    {
        MongoDatabase db;

        public IStoreSubscriptions CreateStore()
        {
            db = MongoDatabase.Create(MongoDbC.ConnectionString);
            return new MongoDbSubscriptionStorage(MongoDbC.ConnectionString, "sagas");
        }

        public void Dispose()
        {
            db.DropCollection("sagas");
        }
    }
}