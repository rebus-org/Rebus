using System;

namespace Rebus.Testing.Events
{
    /// <summary>
    /// Recorded when a subscription was revoked
    /// </summary>
    public class Unsubscribed : FakeBusEvent
    {
        /// <summary>
        /// Gets the type of event that was unsubscribed to
        /// </summary>
        public Type EventType { get; }

        internal Unsubscribed(Type eventType)
        {
            EventType = eventType ?? throw new ArgumentNullException(nameof(eventType));
        }
    }
}