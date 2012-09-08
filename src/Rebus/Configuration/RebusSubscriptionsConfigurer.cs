using Rebus.Persistence.InMemory;
using Rebus.Persistence.SqlServer;
using Rebus.Persistence.Xml;

namespace Rebus.Configuration
{
    public class RebusSubscriptionsConfigurer : BaseConfigurer
    {
        public RebusSubscriptionsConfigurer(ConfigurationBackbone backbone)
            : base(backbone)
        {
        }

        public void Use(IStoreSubscriptions storeSubscriptions)
        {
            Backbone.StoreSubscriptions = storeSubscriptions;
        }

        public void StoreInSqlServer(string connectionstring, string subscriptions)
        {
            Use(new SqlServerSubscriptionStorage(connectionstring, subscriptions));
        }

        public void StoreInMemory()
        {
            Use(new InMemorySubscriptionStorage());
        }

        public void StoreInXmlFile(string xmlFilePath)
        {
            Use(new XmlSubscriptionStorage(xmlFilePath));
        }
    }
}