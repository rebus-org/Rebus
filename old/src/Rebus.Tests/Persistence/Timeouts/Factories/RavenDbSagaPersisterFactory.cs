using Raven.Client.Embedded;
using Rebus.RavenDb;
using Rebus.Timeout;

namespace Rebus.Tests.Persistence.Timeouts.Factories
{
    public class RavenDbTimeoutStorageFactory : ITimeoutStorageFactory
    {
        EmbeddableDocumentStore store;

        public IStoreTimeouts CreateStore()
        {
            store = new EmbeddableDocumentStore
                    {
                        RunInMemory = true
                    };
            store.Initialize();

            return new RavenDbTimeoutStorage(store);
        }

        public void Dispose()
        {
            store.Dispose();
        }
    }
}