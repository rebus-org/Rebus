using Raven.Client.Embedded;
using Rebus.RavenDb;

namespace Rebus.Tests.Persistence.Sagas.Factories
{
    public class RavenDbSagaPersisterFactory : ISagaPersisterFactory
    {
        EmbeddableDocumentStore store;

        public IStoreSagaData CreatePersister()
        {
            store = new EmbeddableDocumentStore
                        {
                            RunInMemory = true
                        };
            store.Initialize();

            return new RavenDbSagaPersister(store);            
        }

        public void Dispose()
        {
            store.Dispose();
        }
    }
}