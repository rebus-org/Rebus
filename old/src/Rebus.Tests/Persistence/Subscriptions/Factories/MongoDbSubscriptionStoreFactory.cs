using MongoDB.Driver;
using Rebus.MongoDb;

namespace Rebus.Tests.Persistence.Subscriptions.Factories
{
    public class MongoDbSubscriptionStoreFactory : ISubscriptionStoreFactory
    {
        MongoDatabase db;

        public IStoreSubscriptions CreateStore()
        {
            db = MongoHelper.GetDatabase(ConnectionStrings.MongoDb);
            return new MongoDbSubscriptionStorage(ConnectionStrings.MongoDb, "sagas");
        }

        public void Dispose()
        {
            db.DropCollection("sagas");
        }
    }
}