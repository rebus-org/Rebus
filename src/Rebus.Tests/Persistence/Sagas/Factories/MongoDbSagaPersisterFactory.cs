using MongoDB.Driver;
using Rebus.MongoDb;

namespace Rebus.Tests.Persistence.Sagas.Factories
{
    public class MongoDbSagaPersisterFactory : ISagaPersisterFactory
    {
        MongoDatabase db;

        public IStoreSagaData CreatePersister()
        {
            db = MongoDatabase.Create(ConnectionStrings.MongoDb);
            return new MongoDbSagaPersister(ConnectionStrings.MongoDb, "sagas");
        }

        public void Dispose()
        {
            db.DropCollection("sagas");
        }
    }
}