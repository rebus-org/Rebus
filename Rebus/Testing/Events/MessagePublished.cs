using System;
using System.Collections.Generic;
using Rebus.Extensions;

namespace Rebus.Testing.Events
{
    /// <summary>
    /// Recorded when an event was published
    /// </summary>
    public abstract class MessagePublished : FakeBusEvent
    {
        internal MessagePublished(object eventMessage, Dictionary<string, string> optionalHeaders)
        {
            if (eventMessage == null) throw new ArgumentNullException(nameof(eventMessage));
            EventMessage = eventMessage;
            OptionalHeaders = optionalHeaders?.Clone();
        }

        /// <summary>
        /// Gets the event message that was published
        /// </summary>
        public object EventMessage { get; }

        /// <summary>
        /// Gets the optional headers if they were supplied, or null if they weren't
        /// </summary>
        public Dictionary<string, string> OptionalHeaders { get; }
    }

    /// <summary>
    /// Recorded when an event was published
    /// </summary>
    public class MessagePublished<TMessage> : MessagePublished
    {
        internal MessagePublished(object eventMessage, Dictionary<string, string> optionalHeaders) : base(eventMessage, optionalHeaders)
        {
            EventMessage = (TMessage) eventMessage;
        }

        /// <summary>
        /// Gets the event message that was published
        /// </summary>
        public new TMessage EventMessage { get; }
    }
}