namespace Rebus2.Messages.Control
{
    public class UnsubscribeRequest
    {
        public string SubscriberAddress { get; set; }
        public string Topic { get; set; }
    }
}