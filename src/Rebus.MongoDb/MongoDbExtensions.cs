using Rebus.Configuration.Configurers;

namespace Rebus.MongoDb
{
    public static class MongoDbExtensions
    {
        public static void StoreInMongoDb(this SagaConfigurer configurer, string connectionString, string collectionName)
        {
            configurer.Use(new MongoDbSagaPersister(connectionString, collectionName));
        }

        public static void StoreInMongoDb(this SubscriptionsConfigurer configurer, string connectionString, string collectionName)
        {
            configurer.Use(new MongoDbSubscriptionStorage(connectionString, collectionName));
        }
    }
}