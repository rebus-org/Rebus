using System.Diagnostics;
using MongoDB.Driver;
using Rebus.MongoDb;
using Rebus.Timeout;

namespace Rebus.Tests.Persistence.Timeouts.Factories
{
    public class MongoDbTimeoutStorageFactory : ITimeoutStorageFactory
    {
        MongoDatabase db;
        Process mongod;

        public IStoreTimeouts CreateStore()
        {
            mongod = MongoHelper.StartServerFromScratch();
            db = MongoHelper.GetDatabase(ConnectionStrings.MongoDb);
            return new MongoDbTimeoutStorage(ConnectionStrings.MongoDb, "timeouts");
        }

        public void Dispose()
        {
            MongoHelper.StopServer(mongod, db);
        }
    }
}