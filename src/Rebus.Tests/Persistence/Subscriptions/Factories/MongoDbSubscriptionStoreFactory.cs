using System.Diagnostics;
using MongoDB.Driver;
using Rebus.MongoDb;

namespace Rebus.Tests.Persistence.Subscriptions.Factories
{
    public class MongoDbSubscriptionStoreFactory : ISubscriptionStoreFactory
    {
        MongoDatabase db;
        Process mongod;

        public IStoreSubscriptions CreateStore()
        {
            mongod = MongoHelper.StartServerFromScratch();
            db = MongoHelper.GetDatabase(ConnectionStrings.MongoDb);
            return new MongoDbSubscriptionStorage(ConnectionStrings.MongoDb, "sagas");
        }

        public void Dispose()
        {
            MongoHelper.StopServer(mongod, db);
        }
    }
}