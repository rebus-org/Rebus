using System;

namespace Rebus
{
    /// <summary>
    /// Static gateway to the current time. Implements the classic fake time pattern
    /// found in all the right places. The name is goofy on purpose to avoid colliding with people's own
    /// Time classes.
    /// </summary>
    public class RebusTimeMachine
    {
        internal static Func<DateTime> OriginalTimeFactoryMethod = () => DateTime.UtcNow;
        internal static Func<DateTime> TimeFactoryMethod = OriginalTimeFactoryMethod;

        /// <summary>
        /// Gets the current time in UTC.
        /// </summary>
        public static DateTime Now()
        {
            return TimeFactoryMethod();
        }

        /// <summary>
        /// Gets the date of today in UTC.
        /// </summary>
        public static DateTime Today()
        {
            return TimeFactoryMethod().Date;
        }
    }
}