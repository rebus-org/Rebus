using System;
using System.Threading.Tasks;
using Rebus.Messages.Control;
using Rebus.Subscriptions;
#pragma warning disable 1998

namespace Rebus.Persistence.InMem
{
    /// <summary>
    /// Implementation of <see cref="ISubscriptionStorage"/> that "persists" subscriptions in memory.
    /// </summary>
    public class InMemorySubscriptionStorage : ISubscriptionStorage
    {
        readonly InMemorySubscriberStore _subscriberStore;

        /// <summary>
        /// Creates the in-mem subscription storage as a decentralized subscription storage with its
        /// own private subscriber store
        /// </summary>
        public InMemorySubscriptionStorage()
        {
            _subscriberStore = new InMemorySubscriberStore();
            IsCentralized = false;
        }

        /// <summary>
        /// Creates the in-mem subscription storage as a centralized subscription storage, using the given
        /// <see cref="InMemorySubscriberStore"/> to share subscriptions
        /// </summary>
        public InMemorySubscriptionStorage(InMemorySubscriberStore subscriberStore)
        {
            if (subscriberStore == null) throw new ArgumentNullException(nameof(subscriberStore));
            _subscriberStore = subscriberStore;
            IsCentralized = true;
        }

        /// <summary>
        /// Gets all destination addresses for the given topic
        /// </summary>
        public async Task<string[]> GetSubscriberAddresses(string topic)
        {
            return _subscriberStore.GetSubscribers(topic);
        }

        /// <summary>
        /// Registers the given <paramref name="subscriberAddress"/> as a subscriber of the given topic
        /// </summary>
        public async Task RegisterSubscriber(string topic, string subscriberAddress)
        {
            _subscriberStore.AddSubscriber(topic, subscriberAddress);
        }

        /// <summary>
        /// Unregisters the given <paramref name="subscriberAddress"/> as a subscriber of the given topic
        /// </summary>
        public async Task UnregisterSubscriber(string topic, string subscriberAddress)
        {
            _subscriberStore.RemoveSubscriber(topic, subscriberAddress);
        }

        /// <summary>
        /// Gets whether the subscription storage is centralized and thus supports bypassing the usual subscription request
        /// (in a fully distributed architecture, a subscription is established by sending a <see cref="SubscribeRequest"/>
        /// to the owner of a given topic, who then remembers the subscriber somehow - if the subscription storage is
        /// centralized, the message exchange can be bypassed, and the subscription can be established directly by
        /// having the subscriber register itself)
        /// </summary>
        public bool IsCentralized { get; }
    }
}