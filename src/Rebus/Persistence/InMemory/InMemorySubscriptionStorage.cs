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

        public void Store(Type messageType, string subscriberInputQueue)
        {
            ConcurrentDictionary<string, object> subscribersForThisType;

            if (!subscribers.TryGetValue(messageType, out subscribersForThisType))
            {
                lock (subscribers)
                {
                    if (!subscribers.TryGetValue(messageType, out subscribersForThisType))
                    {
                        subscribersForThisType = new ConcurrentDictionary<string, object>();
                        subscribers[messageType] = subscribersForThisType;
                    }
                }
            }

            subscribersForThisType.TryAdd(subscriberInputQueue, null);
        }

        public void Remove(Type messageType, string subscriberInputQueue)
        {
            ConcurrentDictionary<string, object> subscribersForThisType;

            if (!subscribers.TryGetValue(messageType, out subscribersForThisType))
                return;

            object temp;
            subscribersForThisType.TryRemove(subscriberInputQueue, out temp);
        }

        public string[] GetSubscribers(Type messageType)
        {
            ConcurrentDictionary<string, object> subscribersForThisType;

            return subscribers.TryGetValue(messageType, out subscribersForThisType)
                       ? subscribersForThisType.Keys.ToArray()
                       : new string[0];
        }
    }
}