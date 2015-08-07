using System;
using System.Globalization;

namespace Rebus.Extensions
{
    static class DateTimeExtensions
    {
        public static TimeSpan ElapsedUntilNow(this DateTime dateTime)
        {
            return DateTime.UtcNow - dateTime.ToUniversalTime();
        }

        public static string ToIso8601DateTimeOffset(this DateTimeOffset dateTimeOffset)
        {
            return dateTimeOffset.ToString("O", CultureInfo.InvariantCulture);
        }

        public static DateTimeOffset ToDateTimeOffset(this string iso8601String)
        {
            DateTimeOffset result;

            if (!DateTimeOffset.TryParseExact(iso8601String, "O", CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out result))
            {
                throw new FormatException(string.Format("Could not parse '{0}' as a proper ISO8601-formatted DateTimeOffset!", iso8601String));
            }

            return result;
        }
    }
}