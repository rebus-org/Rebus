using System.Linq;
using Raven.Client;
using Rebus.Subscriptions;
using System.Threading.Tasks;

namespace Rebus.RavenDb.Subscriptions
{
    /// <summary>
    /// Implementation of <see cref="ISubscriptionStorage"/> that stores subscriptions in RavenDB
    /// </summary>
    public class RavenDbSubscriptionStorage : ISubscriptionStorage
    {
        readonly IDocumentStore _documentStore;

        /// <summary>
        /// Constructs the subscription storage using the specified document store. Can be configured to be centralized
        /// </summary>
        public RavenDbSubscriptionStorage(IDocumentStore documentStore, bool isCentralized)
        {
            _documentStore = documentStore;
            IsCentralized = isCentralized;
        }

        /// <summary>
        /// Gets whether this particular subscription storage is centralized
        /// </summary>
        public bool IsCentralized { get; }

        /// <summary>
        /// Gets all destination addresses for the given topic
        /// </summary>
        public async Task<string[]> GetSubscriberAddresses(string topic)
        {
            using (var session = _documentStore.OpenAsyncSession())
            {
                var topicDocument = await session.LoadAsync<Topic>(topic);

                return topicDocument?.SubscriberAddresses.ToArray()
                    ?? new string[0];
            }
        }

        /// <summary>
        /// Registers the given <paramref name="subscriberAddress"/> as a subscriber of the given topic
        /// </summary>
        public async Task RegisterSubscriber(string topic, string subscriberAddress)
        {
            using (var session = _documentStore.OpenAsyncSession())
            {
                var topicDocument = await session.LoadAsync<Topic>(topic);

                if (topicDocument == null)
                {
                    topicDocument = new Topic(topic, Enumerable.Empty<string>());
                    await session.StoreAsync(topicDocument);
                }

                topicDocument.Register(subscriberAddress);

                await session.SaveChangesAsync();
            }
        }

        /// <summary>
        /// Unregisters the given <paramref name="subscriberAddress"/> as a subscriber of the given topic
        /// </summary>
        public async Task UnregisterSubscriber(string topic, string subscriberAddress)
        {
            using (var session = _documentStore.OpenAsyncSession())
            {
                var topicDocument = await session.LoadAsync<Topic>(topic);

                if (topicDocument == null)
                {
                    topicDocument = new Topic(topic, Enumerable.Empty<string>());
                    await session.StoreAsync(topicDocument);
                }

                topicDocument.Unregister(subscriberAddress);

                await session.SaveChangesAsync();
            }
        }
    }
}