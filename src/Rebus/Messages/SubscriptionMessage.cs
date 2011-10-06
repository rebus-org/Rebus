namespace Rebus.Messages
{
    /// <summary>
    /// Control bus message which is used to tell someone that 
    /// the sender wishes to subscribe to a particular message type.
    /// </summary>
    public class SubscriptionMessage
    {
        public string Type { get; set; }
    }
}