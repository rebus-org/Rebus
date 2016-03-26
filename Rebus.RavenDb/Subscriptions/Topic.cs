using System.Collections.Generic;

namespace Rebus.RavenDb.Subscriptions
{
    /// <summary>
    /// RavenDB document model for a single topic
    /// </summary>
    class Topic
    {
        /// <summary>
        /// Creates the topic
        /// </summary>
        public Topic(string id, IEnumerable<string> subscriberAddresses)
        {
            Id = id;
            SubscriberAddresses = new HashSet<string>(subscriberAddresses);
        }

        /// <summary>
        /// Gets the ID of the document which happens to be the same as the topic
        /// </summary>
        public string Id { get; }

        /// <summary>
        /// Holds the subscribers for this topic
        /// </summary>
        public HashSet<string> SubscriberAddresses { get; }

        /// <summary>
        /// Registers the given <paramref name="address"/> as a subscriber
        /// </summary>
        public void Register(string address)
        {
            SubscriberAddresses.Add(address);
        }

        /// <summary>
        /// Unregisters the given <paramref name="address"/> as a subscriber
        /// </summary>
        public void Unregister(string endpoint)
        {
            SubscriberAddresses.Remove(endpoint);
        }

        /// <summary>
        /// Gets whether the given subscriber is already registered
        /// </summary>
        public bool HasSubscriber(string subscriberAddress)
        {
            return SubscriberAddresses.Contains(subscriberAddress);
        }
    }
}