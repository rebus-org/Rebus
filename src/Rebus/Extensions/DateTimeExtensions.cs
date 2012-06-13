using System;

namespace Rebus.Extensions
{
    public static class DateTimeExtensions
    {
         public static TimeSpan ElapsedUntilNow(this DateTime dateTime)
         {
             return Time.Now() - dateTime;
         }
    }
}