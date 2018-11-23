using System;

namespace Rebus.Time
{
    /// <summary>
    /// Global clock that Rebus services should use if they need to look up the current time
    /// </summary>
    public class RebusTime
    {
        private static readonly Func<DateTimeOffset> DefaultTimeFactory = () => DateTimeOffset.Now;

        private static Func<DateTimeOffset> CurrentTimeFactory = DefaultTimeFactory;

        /// <summary>
        /// Sets the current factory which determines the current time. 
        /// </summary>
        public static void SetFactory(Func<DateTimeOffset> factory)
        {
            CurrentTimeFactory = factory ?? throw new ArgumentNullException(nameof(factory));
        }

        /// <summary>
        /// Reverts the current time factory to the default implementation using <see cref="DateTimeOffset.Now"/>.
        /// </summary>
        public static void Reset()
        {
            CurrentTimeFactory = DefaultTimeFactory;
        }

        /// <summary>
        /// Gets the current time
        /// </summary>
        public static DateTimeOffset Now => CurrentTimeFactory();
    }
}
