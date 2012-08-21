using Rebus.Persistence.InMemory;
using Rebus.Persistence.SqlServer;
using Rebus.Persistence.Xml;

namespace Rebus.Configuration
{
    public class RebusSubscriptionsConfigurer
    {
        readonly ConfigurationBackbone backbone;

        public RebusSubscriptionsConfigurer(ConfigurationBackbone backbone)
        {
            this.backbone = backbone;
        }

        public void Use(IStoreSubscriptions storeSubscriptions)
        {
            backbone.StoreSubscriptions = storeSubscriptions;
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