using MongoDB.Driver;
using Rebus.MongoDb;
using Rebus.Timeout;

namespace Rebus.Tests.Persistence.Timeouts.Factories
{
    public class MongoDbTimeoutStorageFactory : ITimeoutStorageFactory
    {
        MongoDatabase db;

        public IStoreTimeouts CreateStore()
        {
            db = MongoHelper.GetDatabase(ConnectionStrings.MongoDb);
            return new MongoDbTimeoutStorage(ConnectionStrings.MongoDb, "timeouts");
        }

        public void Dispose()
        {
            db.DropCollection("timeouts");
        }
    }
}