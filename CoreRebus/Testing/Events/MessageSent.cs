using System;
using System.Collections.Generic;
using Rebus.Extensions;

namespace Rebus.Testing.Events
{
    /// <summary>
    /// Base event recorded when a message was sent - actual event will be <see cref="MessageSent{TMessage}"/>
    /// </summary>
    public abstract class MessageSent : FakeBusEvent
    {
        internal MessageSent(object commandMessage, Dictionary<string, string> optionalHeaders)
        {
            if (commandMessage == null) throw new ArgumentNullException(nameof(commandMessage));
            OptionalHeaders = optionalHeaders?.Clone();
            CommandMessage = commandMessage;
        }

        /// <summary>
        /// Gets the optional headers if they were supplied, or null if they weren't
        /// </summary>
        public Dictionary<string, string> OptionalHeaders { get; }

        /// <summary>
        /// Gets the message that was sent
        /// </summary>
        public object CommandMessage { get; }
    }

    /// <summary>
    /// Recorded when a message was sent
    /// </summary>
    public class MessageSent<TMessage> : MessageSent
    {
        internal MessageSent(object commandMessage, Dictionary<string, string> optionalHeaders)
            : base(commandMessage, optionalHeaders)
        {
            CommandMessage = (TMessage)commandMessage;
        }

        /// <summary>
        /// Gets the message that was sent
        /// </summary>
        public new TMessage CommandMessage { get; }
    }
}