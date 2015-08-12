using System;
using System.Collections.Concurrent;
using System.Linq;
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
        static readonly StringComparer StringComparer = StringComparer.InvariantCultureIgnoreCase;

        static readonly string[] NoSubscribers = new string[0];

        readonly ConcurrentDictionary<string, ConcurrentDictionary<string, object>> _subscribers
            = new ConcurrentDictionary<string, ConcurrentDictionary<string, object>>(StringComparer);

        /// <summary>
        /// Gets all destination addresses for the given topic
        /// </summary>
        public async Task<string[]> GetSubscriberAddresses(string topic)
        {
            ConcurrentDictionary<string, object> subscriberAddresses;

            return _subscribers.TryGetValue(topic, out subscriberAddresses)
                ? subscriberAddresses.Keys.ToArray()
                : NoSubscribers;
        }

        /// <summary>
        /// Registers the given <paramref name="subscriberAddress"/> as a subscriber of the given topic
        /// </summary>
        public async Task RegisterSubscriber(string topic, string subscriberAddress)
        {
            _subscribers.GetOrAdd(topic, _ => new ConcurrentDictionary<string, object>(StringComparer))
                .TryAdd(subscriberAddress, new object());
        }

        /// <summary>
        /// Unregisters the given <paramref name="subscriberAddress"/> as a subscriber of the given topic
        /// </summary>
        public async Task UnregisterSubscriber(string topic, string subscriberAddress)
        {
            object dummy;

            _subscribers.GetOrAdd(topic, _ => new ConcurrentDictionary<string, object>(StringComparer))
                .TryRemove(subscriberAddress, out dummy);
        }

        /// <summary>
        /// Gets whether the subscription storage is centralized and thus supports bypassing the usual subscription request
        /// (in a fully distributed architecture, a subscription is established by sending a <see cref="SubscribeRequest"/>
        /// to the owner of a given topic, who then remembers the subscriber somehow - if the subscription storage is
        /// centralized, the message exchange can be bypassed, and the subscription can be established directly by
        /// having the subscriber register itself)
        /// </summary>
        public bool IsCentralized
        {
            get
            {
                // in-mem subscription storage is always decentralized
                return false;
            }
        }
    }
}