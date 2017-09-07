using System;
using System.Collections.Generic;
using Rebus.Extensions;

namespace Rebus.Testing.Events
{
    /// <summary>
    /// Recorded when a message was deferred
    /// </summary>
    public abstract class MessageDeferredToSelf : FakeBusEvent
    {
        internal MessageDeferredToSelf(TimeSpan delay, object commandMessage, Dictionary<string, string> optionalHeaders)
        {
            Delay = delay;
            CommandMessage = commandMessage ?? throw new ArgumentNullException(nameof(commandMessage));
            OptionalHeaders = optionalHeaders?.Clone();
        }

        /// <summary>
        /// Gets the time span with which this message was delayed
        /// </summary>
        public TimeSpan Delay { get; }

        /// <summary>
        /// Gets the message that was deferred
        /// </summary>
        public object CommandMessage { get; }

        /// <summary>
        /// Gets the optional headers if they were supplied, or null if they weren't
        /// </summary>
        public Dictionary<string, string> OptionalHeaders { get; }
    }

    /// <summary>
    /// Recorded when a message was deferred
    /// </summary>
    public class MessageDeferredToSelf<TMessage> : MessageDeferredToSelf
    {
        internal MessageDeferredToSelf(TimeSpan delay, object commandMessage, Dictionary<string, string> optionalHeaders)
            : base(delay, commandMessage, optionalHeaders)
        {
            CommandMessage = (TMessage)commandMessage;
        }

        /// <summary>
        /// Gets the message that was deferred
        /// </summary>
        public new TMessage CommandMessage { get; }
    }
}