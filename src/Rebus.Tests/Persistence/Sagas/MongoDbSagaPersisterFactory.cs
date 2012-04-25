using MongoDB.Driver;
using Rebus.MongoDb;

namespace Rebus.Tests.Persistence.Sagas
{
    public class MongoDbSagaPersisterFactory : ISagaPersisterFactory
    {
        MongoDatabase db;

        public IStoreSagaData CreatePersister()
        {
            db = MongoDatabase.Create(MongoDbC.ConnectionString);
            return new MongoDbSagaPersister(MongoDbC.ConnectionString, "sagas");
        }

        public void Dispose()
        {
            db.DropCollection("sagas");
        }
    }
}