namespace Rebus.Messages.Control;

/// <summary>
/// Control message that can be used to establish a subscription of a given topic to the endpoint with the given address.
/// The receiving endpoint must either be the one publishing messages with the given topic, or it must have a connection
/// to a centralized subscription storage
/// </summary>
public class SubscribeRequest
{
    /// <summary>
    /// Specifies the globally addressable queue address of the subscriber to enlist for the given topic
    /// </summary>
    public string SubscriberAddress { get; set; }
        
    /// <summary>
    /// Specifis the topic for which the subscriber wishes to subscribe
    /// </summary>
    public string Topic { get; set; }
}