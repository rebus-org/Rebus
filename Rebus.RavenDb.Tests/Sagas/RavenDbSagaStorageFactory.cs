using Raven.Client.Embedded;
using Rebus.RavenDb.Sagas;
using Rebus.Sagas;
using Rebus.Tests.Contracts.Sagas;

namespace Rebus.RavenDb.Tests.Sagas
{
    public class RavenDbSagaStorageFactory : ISagaStorageFactory
    {
        readonly EmbeddableDocumentStore _documentStore;

        public RavenDbSagaStorageFactory()
        {
            _documentStore = new EmbeddableDocumentStore
            {
                RunInMemory = true,
            };

            _documentStore.Configuration.Storage.Voron.AllowOn32Bits = true;

            _documentStore.Initialize();
        }

        public EmbeddableDocumentStore DocumentStore => _documentStore;

        public ISagaStorage GetSagaStorage()
        {
            return new RavenDbSagaStorage(_documentStore);
        }

        public void CleanUp()
        {
            _documentStore.Dispose();
        }
    }
}