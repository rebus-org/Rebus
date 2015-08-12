using Rebus.Persistence.InMemory;
using Rebus.Persistence.SqlServer;
using Rebus.Persistence.Xml;

namespace Rebus.Configuration
{
    /// <summary>
    /// Configurer to configure which kind of subscription storage should be used
    /// </summary>
    public class RebusSubscriptionsConfigurer : BaseConfigurer
    {
        /// <summary>
        /// Constructs the confiurer
        /// </summary>
        public RebusSubscriptionsConfigurer(ConfigurationBackbone backbone)
            : base(backbone)
        {
        }

        /// <summary>
        /// Uses the specified implementation of <see cref="IStoreSubscriptions"/> to store
        /// subscriptions
        /// </summary>
        public void Use(IStoreSubscriptions storeSubscriptions)
        {
            Backbone.StoreSubscriptions = storeSubscriptions;
        }

        /// <summary>
        /// Configures Rebus to use <see cref="SqlServerSubscriptionStorage"/> to store subscriptions
        /// </summary>
        public SqlServerSubscriptionStorageFluentConfigurer StoreInSqlServer(string connectionstring, string subscriptions)
        {
            var storage = new SqlServerSubscriptionStorage(connectionstring, subscriptions);
            Use(storage);
            return new SqlServerSubscriptionStorageFluentConfigurer(storage);
        }

        /// <summary>
        /// Configures Rebus to use <see cref="InMemorySubscriptionStorage"/> to store subscriptions
        /// </summary>
        public void StoreInMemory()
        {
            Use(new InMemorySubscriptionStorage());
        }

        /// <summary>
        /// Configures Rebus to use <see cref="XmlSubscriptionStorage"/> to store subscriptions
        /// </summary>
        public void StoreInXmlFile(string xmlFilePath)
        {
            Use(new XmlSubscriptionStorage(xmlFilePath));
        }
    }
}