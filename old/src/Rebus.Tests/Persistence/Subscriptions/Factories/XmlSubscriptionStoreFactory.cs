using System;
using System.IO;
using Rebus.Persistence.Xml;

namespace Rebus.Tests.Persistence.Subscriptions.Factories
{
    public class XmlSubscriptionStoreFactory : ISubscriptionStoreFactory
    {
        public IStoreSubscriptions CreateStore()
        {
            var path = Environment.CurrentDirectory;
            var filename = "subscriptions.xml";
            var filePath = Path.Combine(path, filename);

            var store = new XmlSubscriptionStorage(filePath);
            store.ClearAllSubscriptions();
            return store;
        }

        public void Dispose()
        {
        }
    }
}