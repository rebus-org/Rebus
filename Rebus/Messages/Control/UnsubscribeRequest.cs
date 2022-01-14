namespace Rebus.Messages.Control;

/// <summary>
/// Control message that can be used to end a subscription of a given topic to the endpoint with the given address.
/// The receiving endpoint must either be the one publishing messages with the given topic, or it must have a connection
/// to a centralized subscription storage
/// </summary>
public class UnsubscribeRequest
{
    /// <summary>
    /// Specifies the globally addressable queue address of the subscriber to remove for the given topic
    /// </summary>
    public string SubscriberAddress { get; set; }

    /// <summary>
    /// Specifis the topic from which the subscriber wishes to unsubscribe
    /// </summary>
    public string Topic { get; set; }
}