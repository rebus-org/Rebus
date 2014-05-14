using System.Diagnostics;
using MongoDB.Driver;
using Rebus.MongoDb;

namespace Rebus.Tests.Persistence.Sagas.Factories
{
    public class MongoDbSagaPersisterFactory : ISagaPersisterFactory
    {
        MongoDatabase db;
        Process mongod;

        public IStoreSagaData CreatePersister()
        {
            mongod = MongoHelper.StartServerFromScratch();

            db = MongoHelper.GetDatabase(ConnectionStrings.MongoDb);
            db.Drop();

            return new MongoDbSagaPersister(ConnectionStrings.MongoDb)
                .AllowAutomaticSagaCollectionNames();
        }

        public void Dispose()
        {
            MongoHelper.StopServer(mongod, db);
        }
    }
}