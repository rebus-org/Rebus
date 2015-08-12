using System;
using System.Collections.Concurrent;
using System.Linq;

namespace Rebus.Persistence.InMemory
{
    /// <summary>
    /// Implementation of <see cref="IStoreSubscriptions"/> that stores the type -> endpoint mappings in
    /// an in-memory dictionary
    /// </summary>
    public class InMemorySubscriptionStorage : IStoreSubscriptions
    {
        readonly ConcurrentDictionary<Type, ConcurrentDictionary<string, object>> subscribers = new ConcurrentDictionary<Type, ConcurrentDictionary<string, object>>();

        /// <summary>
        /// Stores a subscription for the given message type and the given endpoint in memory
        /// </summary>
        public void Store(Type eventType, string subscriberInputQueue)
        {
            ConcurrentDictionary<string, object> subscribersForThisType;

            if (!subscribers.TryGetValue(eventType, out subscribersForThisType))
            {
                lock (subscribers)
                {
                    if (!subscribers.TryGetValue(eventType, out subscribersForThisType))
                    {
                        subscribersForThisType = new ConcurrentDictionary<string, object>();
                        subscribers[eventType] = subscribersForThisType;
                    }
                }
            }

            subscribersForThisType.TryAdd(subscriberInputQueue, null);
        }

        /// <summary>
        /// Removes the subscription (if any) for the given message type and the given endpoint from memory
        /// </summary>
        public void Remove(Type eventType, string subscriberInputQueue)
        {
            ConcurrentDictionary<string, object> subscribersForThisType;

            if (!subscribers.TryGetValue(eventType, out subscribersForThisType))
                return;

            object temp;
            subscribersForThisType.TryRemove(subscriberInputQueue, out temp);
        }

        /// <summary>
        /// Gets from memory an array of endpoints that subscribe to the given message type
        /// </summary>
        public string[] GetSubscribers(Type eventType)
        {
            ConcurrentDictionary<string, object> subscribersForThisType;

            return subscribers.TryGetValue(eventType, out subscribersForThisType)
                       ? subscribersForThisType.Keys.ToArray()
                       : new string[0];
        }
    }
}