namespace Rebus.Messages.Control
{
    /// <summary>
    /// Control message that can be used to end a subscription of a given topic to the endpoint with the given address.
    /// The receiving endpoint must either be the one publishing messages with the given topic, or it must have a connection
    /// to a centralized subscription storage
    /// </summary>
    public class UnsubscribeRequest
    {
        public string SubscriberAddress { get; set; }
        public string Topic { get; set; }
    }
}