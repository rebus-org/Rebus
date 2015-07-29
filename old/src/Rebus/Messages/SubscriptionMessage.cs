using System;

namespace Rebus.Messages
{
    /// <summary>
    /// Control bus message which is used to tell someone that 
    /// the sender wishes to subscribe to a particular message type.
    /// </summary>
    [Serializable]
    public class SubscriptionMessage : IRebusControlMessage
    {
        /// <summary>
        /// Text description of the message type in question
        /// </summary>
        public string Type { get; set; }
        
        /// <summary>
        /// Indicates whether the specified message type should be subscribed/unsubscribed
        /// </summary>
        public SubscribeAction Action { get; set; }
    }

    /// <summary>
    /// Describes what the subscription message is actually supposed to do.
    /// </summary>
    [Serializable]
    public enum SubscribeAction
    {
        /// <summary>
        /// Indicates that a subscription shoule be set up
        /// </summary>
        Subscribe,

        /// <summary>
        /// Indicates that a subscription shoule be torn down
        /// </summary>
        Unsubscribe,
    }
}