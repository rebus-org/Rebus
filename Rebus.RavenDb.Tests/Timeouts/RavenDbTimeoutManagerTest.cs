using NUnit.Framework;
using Raven.Client.Embedded;
using Rebus.RavenDb.Timouts;
using Rebus.Tests.Contracts.Timeouts;
using Rebus.Timeouts;

namespace Rebus.RavenDb.Tests.Timeouts
{
    [TestFixture]
    public class RavenDbTimeoutManagerTest : BasicStoreAndRetrieveOperations<RavenDbTimoutManagerFactory>
    {
    }

    public class RavenDbTimoutManagerFactory : ITimeoutManagerFactory
    {
        private EmbeddableDocumentStore _documentStore;

        public ITimeoutManager Create()
        {

            _documentStore = new EmbeddableDocumentStore()
            {
                RunInMemory = true,
            };

            _documentStore.Configuration.Storage.Voron.AllowOn32Bits = true;

            _documentStore.Initialize();
            _documentStore.ExecuteIndex(new TimeoutIndex());

            return new RavenDbTimeoutManager(_documentStore);
        }

        public void Cleanup()
        {
            _documentStore.Dispose();
            _documentStore = null;
        }
    }
}
