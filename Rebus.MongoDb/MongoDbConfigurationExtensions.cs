using System;
using MongoDB.Driver;
using Rebus.Config;
using Rebus.MongoDb.Sagas;
using Rebus.MongoDb.Subscriptions;
using Rebus.MongoDb.Timeouts;
using Rebus.Sagas;
using Rebus.Subscriptions;
using Rebus.Timeouts;

namespace Rebus.MongoDb
{
    public static class MongoDbConfigurationExtensions
    {
        /// <summary>
        /// Configures Rebus to use MongoDB to store sagas, using the specified collection name resolver function. If the collection name resolver is omitted,
        /// collection names will be determined by using the <code>Name</code> property of the saga data's <see cref="Type"/>
        /// </summary>
        public static void StoreInMongoDb(this StandardConfigurer<ISagaStorage> configurer, MongoDatabase mongoDatabase, Func<Type, string> collectionNameResolver = null)
        {
            if (configurer == null) throw new ArgumentNullException("configurer");
            if (mongoDatabase == null) throw new ArgumentNullException("mongoDatabase");

            collectionNameResolver = collectionNameResolver
                                     ?? (sagaDataType => sagaDataType.Name);

            configurer.Register(c =>
            {
                var sagaStorage = new MongoDbSagaStorage(mongoDatabase, collectionNameResolver);

                return sagaStorage;
            });
        }

        /// <summary>
        /// Configures Rebus to use MongoDB to store subscriptions. Use <see cref="isCentralized"/> = true to indicate whether it's OK to short-circuit
        /// subscribing and unsubscribing by manipulating the subscription directly from the subscriber or just let it default to false to preserve the
        /// default behavior.
        /// </summary>
        public static void StoreInMongoDb(this StandardConfigurer<ISubscriptionStorage> configurer, MongoDatabase mongoDatabase, string collectionName, bool isCentralized = false)
        {
            if (configurer == null) throw new ArgumentNullException("configurer");
            if (mongoDatabase == null) throw new ArgumentNullException("mongoDatabase");
            if (collectionName == null) throw new ArgumentNullException("collectionName");

            configurer.Register(c =>
            {
                var subscriptionStorage = new MongoDbSubscriptionStorage(mongoDatabase, collectionName, isCentralized);

                return subscriptionStorage;
            });
        }

        /// <summary>
        /// Configures Rebus to use MongoDB to store timeouts.
        /// </summary>
        public static void StoreInMongoDb(this StandardConfigurer<ITimeoutManager> configurer, MongoDatabase mongoDatabase, string collectionName)
        {
            configurer.Register(c =>
            {
                var subscriptionStorage = new MongoDbTimeoutManager(mongoDatabase, collectionName);

                return subscriptionStorage;
            });
        }
    }
}