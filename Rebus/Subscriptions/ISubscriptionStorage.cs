using System.Threading.Tasks;
using Rebus.Messages.Control;

namespace Rebus.Subscriptions;

/// <summary>
/// Abstraction that handles how subscriptions are stored
/// </summary>
public interface ISubscriptionStorage
{
    /// <summary>
    /// Gets all destination addresses for the given topic
    /// </summary>
    Task<string[]> GetSubscriberAddresses(string topic);

    /// <summary>
    /// Registers the given <paramref name="subscriberAddress"/> as a subscriber of the given topic
    /// </summary>
    Task RegisterSubscriber(string topic, string subscriberAddress);

    /// <summary>
    /// Unregisters the given <paramref name="subscriberAddress"/> as a subscriber of the given topic
    /// </summary>
    Task UnregisterSubscriber(string topic, string subscriberAddress);

    /// <summary>
    /// Gets whether the subscription storage is centralized and thus supports bypassing the usual subscription request
    /// (in a fully distributed architecture, a subscription is established by sending a <see cref="SubscribeRequest"/>
    /// to the owner of a given topic, who then remembers the subscriber somehow - if the subscription storage is
    /// centralized, the message exchange can be bypassed, and the subscription can be established directly by
    /// having the subscriber register itself)
    /// </summary>
    bool IsCentralized { get; }
}