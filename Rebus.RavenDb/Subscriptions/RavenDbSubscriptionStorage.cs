using Raven.Client;
using Rebus.Subscriptions;
using System.Threading.Tasks;

namespace Rebus.RavenDb.Subscriptions
{
    public class RavenDbSubscriptionStorage : ISubscriptionStorage
    {
        private readonly IDocumentStore _documentStore;

        public RavenDbSubscriptionStorage(IDocumentStore documentStore, bool isCentralized = false)
        {
            _documentStore = documentStore;
            IsCentralized = isCentralized;
        }

        public async Task<string[]> GetSubscriberAddresses(string topic)
        {
            using (var session = _documentStore.OpenAsyncSession())
            {
                await EnsureSubscriptionsExists(session);

                var subscriptions = await session.LoadAsync<Subscription>(Subscription.Id);

                return await Task.FromResult(subscriptions.GetSubscriberAddresses(topic));
            }
        }

        public async Task RegisterSubscriber(string topic, string subscriberAddress)
        {
            using (var session = _documentStore.OpenAsyncSession())
            {
                await EnsureSubscriptionsExists(session);

                var subscriptions = await session.LoadAsync<Subscription>(Subscription.Id);
                subscriptions.RegisterSubscriber(topic, subscriberAddress);

                await session.SaveChangesAsync();
            }
        }

        public async Task UnregisterSubscriber(string topic, string subscriberAddress)
        {
            using (var session = _documentStore.OpenAsyncSession())
            {
                await EnsureSubscriptionsExists(session);

                var subscriptions = await session.LoadAsync<Subscription>(Subscription.Id);
                subscriptions.UnregisterSubscriber(topic, subscriberAddress);

                await session.SaveChangesAsync();
            }
        }

        private async Task EnsureSubscriptionsExists(IAsyncDocumentSession session)
        {
            var subscription = await session.LoadAsync<Subscription>(Subscription.Id);
            if (subscription == null)
            {
                subscription = new Subscription();
                await session.StoreAsync(subscription);
                await session.SaveChangesAsync();
            }
        }

        public bool IsCentralized { get; private set; }
    }
}