using System;

namespace Rebus
{
    public class Time
    {
        internal static Func<DateTime> OriginalTimeFactoryMethod = () => DateTime.UtcNow;
        internal static Func<DateTime> TimeFactoryMethod = OriginalTimeFactoryMethod;

        public static DateTime Now()
        {
            return TimeFactoryMethod();
        }

        public static DateTime Today()
        {
            return TimeFactoryMethod().Date;
        }
    }
}