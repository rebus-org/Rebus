using NUnit.Framework;
using Raven.Client.Embedded;
using Rebus.RavenDb.Subscriptions;
using Rebus.Subscriptions;
using Rebus.Tests.Contracts.Subscriptions;

namespace Rebus.RavenDb.Tests.Subscriptions
{
    [TestFixture]
    public class RavenDbSubscriptionStorageTests : BasicSubscriptionOperations<RavenDbSubscriptionStorageFactory>
    {
    }

    public class RavenDbSubscriptionStorageFactory : ISubscriptionStorageFactory
    {
        private EmbeddableDocumentStore _documentStore;

        public ISubscriptionStorage Create()
        {
            _documentStore = new EmbeddableDocumentStore()
           {
               RunInMemory = true,
           };

            _documentStore.Configuration.Storage.Voron.AllowOn32Bits = true;

            _documentStore.Initialize();

            return new RavenDbSubscriptionStorage(_documentStore);

        }

        public void Cleanup()
        {
            _documentStore.Dispose();
            _documentStore = null;
        }
    }
}
