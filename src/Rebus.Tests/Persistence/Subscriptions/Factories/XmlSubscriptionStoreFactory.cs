using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace Rebus.Tests.Persistence.Subscriptions.Factories
{
    public class XmlSubscriptionStoreFactory : ISubscriptionStoreFactory
    {
        public IStoreSubscriptions CreateStore()
        {
            var path = Environment.CurrentDirectory;
            var filename = "subscriptions.xml";
            var filePath = Path.Combine(path, filename);

            var store = new Rebus.Xml.XmlSubscriptionStorage(filePath);
            store.ClearAllSubscriptions();
            return store;
        }

        public void Dispose()
        {
        }
    }
}