namespace Rebus.Messages.Control
{
    /// <summary>
    /// Control message that can be used to establish a subscription of a given topic to the endpoint with the given address.
    /// The receiving endpoint must either be the one publishing messages with the given topic, or it must have a connection
    /// to a centralized subscription storage
    /// </summary>
    public class SubscribeRequest
    {
        public string SubscriberAddress { get; set; }
        public string Topic { get; set; }
    }
}