using System;

namespace Rebus.Testing.Events
{
    /// <summary>
    /// Recorded when a subscription was made
    /// </summary>
    public class Subscribed : FakeBusEvent
    {
        /// <summary>
        /// Gets the type of event that was subscribed to
        /// </summary>
        public Type EventType { get; }

        internal Subscribed(Type eventType)
        {
            EventType = eventType ?? throw new ArgumentNullException(nameof(eventType));
        }
    }
}