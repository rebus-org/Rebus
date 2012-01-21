namespace Rebus.Messages
{
    /// <summary>
    /// Control bus message which is used to tell someone that 
    /// the sender wishes to subscribe to a particular message type.
    /// </summary>
    public class SubscriptionMessage : IRebusControlMessage
    {
        public string Type { get; set; }
        public SubscribeAction Action { get; set; }
    }

    /// <summary>
    /// Describes what the subscription message is actually supposed to do.
    /// </summary>
    public enum SubscribeAction
    {
        Subscribe,
        Unsubscribe,
    }
}