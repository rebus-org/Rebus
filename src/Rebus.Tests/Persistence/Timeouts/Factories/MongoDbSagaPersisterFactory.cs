using MongoDB.Driver;
using Rebus.MongoDb;
using Rebus.Tests.Persistence.Sagas;
using Rebus.Timeout;

namespace Rebus.Tests.Persistence.Timeouts.Factories
{
    public class MongoDbTimeoutStorageFactory : ITimeoutStorageFactory
    {
        MongoDatabase db;

        public IStoreTimeouts CreateStore()
        {
            db = MongoDatabase.Create(ConnectionStrings.MongoDb);
            return new MongoDbTimeoutStorage(ConnectionStrings.MongoDb, "timeouts");
        }

        public void Dispose()
        {
            db.DropCollection("timeouts");
        }
    }
}