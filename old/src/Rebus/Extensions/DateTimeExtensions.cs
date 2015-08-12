using System;

namespace Rebus.Extensions
{
    internal static class DateTimeExtensions
    {
        public static TimeSpan ElapsedUntilNow(this DateTime dateTime)
         {
             return RebusTimeMachine.Now() - dateTime;
         }
    }
}