using System;

namespace Rebus
{
    /// <summary>
    /// Implement this in order to affect how subscriptions are stored.
    /// </summary>
    public interface IStoreSubscriptions
    {
        void Save(Type messageType, string subscriberInputQueue);
        string[] GetSubscribers(Type messageType);
    }
}