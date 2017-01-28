using System;
using Rebus.Time;

namespace Rebus.Testing.Events
{
    /// <summary>
    /// Base type of all events that a <see cref="FakeBus"/> can record.
    /// </summary>
    public abstract class FakeBusEvent
    {
        /// <summary>
        /// Gets the time of when the event was recorded
        /// </summary>
        public DateTimeOffset Time { get; } = RebusTime.Now;

        internal FakeBusEvent() { }

        /// <summary>
        /// Gets a nice string representation of this particular fake bus event
        /// </summary>
        public override string ToString()
        {
            return $"{GetType().Name}";
        }
    }
}