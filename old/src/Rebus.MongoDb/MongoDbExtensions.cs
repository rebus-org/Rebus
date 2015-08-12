using Rebus.Configuration;

namespace Rebus.MongoDb
{
    /// <summary>
    /// Configuration extensions to allow for fluently configuring Rebus with MongoDB
    /// </summary>
    public static class MongoDbExtensions
    {
        /// <summary>
        /// Configures Rebus to store subscriptions in the given collection in MongoDB, in the database specified by the connection string
        /// </summary>
        public static void StoreInMongoDb(this RebusSubscriptionsConfigurer configurer, string connectionString, string collectionName)
        {
            configurer.Use(new MongoDbSubscriptionStorage(connectionString, collectionName));
        }

        /// <summary>
        /// Configures Rebus to store saga data in MongoDB, in the database specified by the connection string
        /// </summary>
        public static MongoDbSagaPersisterConfigurationBuilder StoreInMongoDb(this RebusSagasConfigurer configurer, string connectionString)
        {
            var persister = new MongoDbSagaPersister(connectionString);
            configurer.Use(persister);
            return new MongoDbSagaPersisterConfigurationBuilder(persister);
        }

        /// <summary>
        /// Configures Rebus to store timeouts internally in the given collection in MongoDB, in the database specified by the connection string
        /// </summary>
        public static void StoreInMongoDb(this RebusTimeoutsConfigurer configurer, string connectionString,
                                          string collectionName)
        {
            configurer.Use(new MongoDbTimeoutStorage(connectionString, collectionName));
        }

        /// <summary>
        /// Fluent builder class that forwards calls to the configured saga persister instance
        /// </summary>
        public class MongoDbSagaPersisterConfigurationBuilder
        {
            readonly MongoDbSagaPersister mongoDbSagaPersister;

            internal MongoDbSagaPersisterConfigurationBuilder(MongoDbSagaPersister mongoDbSagaPersister)
            {
                this.mongoDbSagaPersister = mongoDbSagaPersister;
            }

            /// <summary>
            /// Configures the saga persister to store saga data of the given type in the specified collection
            /// </summary>
            public MongoDbSagaPersister SetCollectionName<TSagaData>(string collectionName) where TSagaData : ISagaData
            {
                return mongoDbSagaPersister.SetCollectionName<TSagaData>(collectionName);
            }

            /// <summary>
            /// Turns on automatic saga collection name generation - will kick in for all saga data types that have
            /// not had a collection name explicitly configured
            /// </summary>
            public MongoDbSagaPersister AllowAutomaticSagaCollectionNames()
            {
                return mongoDbSagaPersister.AllowAutomaticSagaCollectionNames();
            }
        }
    }
}