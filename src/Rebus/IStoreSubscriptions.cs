using System;

namespace Rebus
{
    /// <summary>
    /// Implement this in order to affect how subscriptions are stored.
    /// </summary>
    public interface IStoreSubscriptions
    {
        /// <summary>
        /// Saves the association between the given message type and the specified endpoint name.
        /// </summary>
        void Store(Type messageType, string subscriberInputQueue);

        /// <summary>
        /// Removes the association between the given message type and the specified endpoint name.
        /// </summary>
        void Remove(Type messageType, string subscriberInputQueue);

        /// <summary>
        /// Returns the endpoint names for the given message type.
        /// </summary>
        string[] GetSubscribers(Type messageType);
    }
}