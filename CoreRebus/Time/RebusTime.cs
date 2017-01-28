using System;

namespace Rebus.Time
{
    /// <summary>
    /// Global clock that Rebus services should use if they need to look up the current time
    /// </summary>
    public class RebusTime
    {
        internal static readonly Func<DateTimeOffset> DefaultTimeFactory = () => DateTimeOffset.Now;

        internal static Func<DateTimeOffset> CurrentTimeFactory = DefaultTimeFactory;

        internal static void Reset()
        {
            CurrentTimeFactory = DefaultTimeFactory;
        }

        /// <summary>
        /// Gets the current time
        /// </summary>
        public static DateTimeOffset Now => CurrentTimeFactory();
    }
}