using System;
using System.Collections.Concurrent;
using Raven.Client.Embedded;
using Rebus.Logging;
using Rebus.RavenDb.Subscriptions;
using Rebus.Subscriptions;
using Rebus.Tests.Contracts.Subscriptions;

namespace Rebus.RavenDb.Tests.Subscriptions
{
    public class RavenDbSubscriptionStorageFactory : ISubscriptionStorageFactory
    {
        readonly ConcurrentStack<IDisposable> _disposables = new ConcurrentStack<IDisposable>();

        public ISubscriptionStorage Create()
        {
            var documentStore = new EmbeddableDocumentStore
            {
                RunInMemory = true,
            };

            documentStore.Configuration.Storage.Voron.AllowOn32Bits = true;
            documentStore.Initialize();

            _disposables.Push(documentStore);

            return new RavenDbSubscriptionStorage(documentStore, true, new ConsoleLoggerFactory(false));
        }

        public void Cleanup()
        {
            IDisposable disposable;

            while (_disposables.TryPop(out disposable))
            {
                disposable.Dispose();
            }
        }
    }
}