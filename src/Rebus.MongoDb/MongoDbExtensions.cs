using Rebus.Configuration;

namespace Rebus.MongoDb
{
    public static class MongoDbExtensions
    {
        public static void StoreInMongoDb(this RebusSubscriptionsConfigurer configurer, string connectionString, string collectionName)
        {
            configurer.Use(new MongoDbSubscriptionStorage(connectionString, collectionName));
        }

        public static MongoDbSagaPersisterConfigurationBuilder StoreInMongoDb(this RebusSagasConfigurer configurer, string connectionString)
        {
            var persister = new MongoDbSagaPersister(connectionString);
            configurer.Use(persister);
            return new MongoDbSagaPersisterConfigurationBuilder(persister);
        }

        public class MongoDbSagaPersisterConfigurationBuilder
        {
            readonly MongoDbSagaPersister mongoDbSagaPersister;

            public MongoDbSagaPersisterConfigurationBuilder(MongoDbSagaPersister mongoDbSagaPersister)
            {
                this.mongoDbSagaPersister = mongoDbSagaPersister;
            }

            public MongoDbSagaPersister SetCollectionName<TSagaData>(string collectionName) where TSagaData : ISagaData
            {
                return mongoDbSagaPersister.SetCollectionName<TSagaData>(collectionName);
            }

            public MongoDbSagaPersister AllowAutomaticSagaCollectionNames()
            {
                return mongoDbSagaPersister.AllowAutomaticSagaCollectionNames();
            }
        }
    }
}