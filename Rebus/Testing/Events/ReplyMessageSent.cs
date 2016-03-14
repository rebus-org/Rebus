using System;
using System.Collections.Generic;
using Rebus.Extensions;

namespace Rebus.Testing.Events
{
    /// <summary>
    /// Recorded when a reply message was sent
    /// </summary>
    public abstract class ReplyMessageSent : FakeBusEvent
    {
        internal ReplyMessageSent(object replyMessage, Dictionary<string, string> optionalHeaders)
        {
            if (replyMessage == null) throw new ArgumentNullException(nameof(replyMessage));
            ReplyMessage = replyMessage;
            OptionalHeaders = optionalHeaders?.Clone();
        }

        /// <summary>
        /// Gets the message that was sent
        /// </summary>
        public object ReplyMessage { get; }

        /// <summary>
        /// Gets the optional headers if they were supplied, or null if they weren't
        /// </summary>
        public Dictionary<string, string> OptionalHeaders { get; }
    }

    /// <summary>
    /// Recorded when a reply message was sent
    /// </summary>
    public class ReplyMessageSent<TMessage> : ReplyMessageSent
    {
        internal ReplyMessageSent(object replyMessage, Dictionary<string, string> optionalHeaders) : base(replyMessage, optionalHeaders)
        {
            ReplyMessage = (TMessage) replyMessage;
        }

        /// <summary>
        /// Gets the message that was sent
        /// </summary>
        public new TMessage ReplyMessage { get; }
    }
}