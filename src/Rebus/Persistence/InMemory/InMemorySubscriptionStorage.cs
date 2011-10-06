using System;
using System.Collections.Concurrent;

namespace Rebus.Persistence.InMemory
{
    public class InMemorySubscriptionStorage : IStoreSubscriptions
    {
        readonly ConcurrentDictionary<Type, ConcurrentBag<string>> subscribers = new ConcurrentDictionary<Type, ConcurrentBag<string>>();

        public void Save(Type messageType, string subscriberInputQueue)
        {
            ConcurrentBag<string> subscribersForThisType;

            if (!subscribers.TryGetValue(messageType, out subscribersForThisType))
            {
                lock(subscribers)
                {
                    if (!subscribers.TryGetValue(messageType, out subscribersForThisType))
                    {
                        subscribersForThisType  = new ConcurrentBag<string>();
                        subscribers[messageType] = subscribersForThisType;
                    }
                }
            }

            subscribersForThisType.Add(subscriberInputQueue);
        }

        public string[] GetSubscribers(Type messageType)
        {
            ConcurrentBag<string> subscribersForThisType;

            return subscribers.TryGetValue(messageType, out subscribersForThisType)
                       ? subscribersForThisType.ToArray()
                       : new string[0];
        }
    }
}