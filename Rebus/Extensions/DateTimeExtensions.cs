using System;

namespace Rebus.Extensions
{
    static class DateTimeExtensions
    {
        public static TimeSpan ElapsedUntilNow(this DateTime dateTime)
        {
            return DateTime.UtcNow - dateTime.ToUniversalTime();
        }     
    }
}