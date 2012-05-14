using Rebus.Configuration.Configurers;

namespace Rebus.MongoDb
{
    public static class MongoDbExtensions
    {
        public static MongoSagaCollectionMapper StoreInMongoDb(this SagaConfigurer configurer, string connectionString)
        {
            var persister = new MongoDbSagaPersister(connectionString);
            configurer.Use(persister);
            return new MongoSagaCollectionMapper(persister);
        }

        public static void StoreInMongoDb(this SubscriptionsConfigurer configurer, string connectionString, string collectionName)
        {
            configurer.Use(new MongoDbSubscriptionStorage(connectionString, collectionName));
        }
    }

    public class MongoSagaCollectionMapper
    {
        readonly MongoDbSagaPersister mongoDbSagaPersister;

        public MongoSagaCollectionMapper(MongoDbSagaPersister mongoDbSagaPersister)
        {
            this.mongoDbSagaPersister = mongoDbSagaPersister;
        }

        public MongoDbSagaPersister SetCollectionName<TSagaData>(string collectionName)
        {
            return mongoDbSagaPersister.SetCollectionName<TSagaData>(collectionName);
        }
    }
}