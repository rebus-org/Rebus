using MongoDB.Driver;
using Rebus.MongoDb;

namespace Rebus.Tests.Persistence.Sagas.Factories
{
    public class MongoDbSagaPersisterFactory : ISagaPersisterFactory
    {
        MongoDatabase db;

        public IStoreSagaData CreatePersister()
        {
            db = MongoHelper.GetDatabase(ConnectionStrings.MongoDb);
            
            db.Drop();

            return new MongoDbSagaPersister(ConnectionStrings.MongoDb)
                .AllowAutomaticSagaCollectionNames();
        }

        public void Dispose()
        {
            db.Drop();
        }
    }
}