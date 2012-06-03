using Raven.Client.Embedded;
using Rebus.RavenDb;

namespace Rebus.Tests.Persistence.Subscriptions.Factories
{
    public class RavenDbSubscriptionStoreFactory : ISubscriptionStoreFactory
    {
        EmbeddableDocumentStore store;

        public IStoreSubscriptions CreateStore()
        {
            store = new EmbeddableDocumentStore
                        {
                            RunInMemory = true
                        };
            store.Initialize();

            return new RavenDbSubscriptionStorage(store, "subscriptions");
        }

        public void Dispose()
        {
            store.Dispose();
        }
    }
}