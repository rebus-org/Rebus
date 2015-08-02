using Raven.Client;
using Rebus.Config;
using Rebus.RavenDb.Subscriptions;
using Rebus.RavenDb.Timouts;
using Rebus.Subscriptions;
using Rebus.Timeouts;
using System;

namespace Rebus.RavenDb
{
    /// <summary>
    /// Configuration extensions for RavenDB persistence
    /// </summary>
    public static class RavenDbConfigurationExtensions
    {
        /// <summary>
        /// Configures Rebus to use RavenDb to store subscriptions. Use <see cref="isCentralized"/> = true to indicate whether it's OK to short-circuit
        /// subscribing and unsubscribing by manipulating the subscription directly from the subscriber or just let it default to false to preserve the
        /// default behavior.
        /// </summary>
        public static void StoreInRavenDb(this StandardConfigurer<ISubscriptionStorage> configurer, IDocumentStore documentStore, bool isCentralized = false)
        {
            if (configurer == null) throw new ArgumentNullException("configurer");
            if (documentStore == null) throw new ArgumentNullException("documentStore");

            configurer.Register(c =>
            {
                var subscriptionStorage = new RavenDbSubscriptionStorage(documentStore, isCentralized);
                return subscriptionStorage;
            });
        }

        /// <summary>
        /// Configures Rebus to use RavenDb to store timeouts.
        /// </summary>
        public static void StoreInRavenDb(this StandardConfigurer<ITimeoutManager> configurer,
            IDocumentStore documentStore)
        {
            if (configurer == null) throw new ArgumentNullException("configurer");
            if (documentStore == null) throw new ArgumentNullException("documentStore");

            documentStore.ExecuteIndex(new TimeoutIndex());

            configurer.Register(c =>
            {
                var timeoutManager = new RavenDbTimeoutManager(documentStore);
                return timeoutManager;
            });
        }
    }
}
