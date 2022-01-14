using System;
using System.Globalization;
using Rebus.Time;

namespace Rebus.Extensions;

/// <summary>
/// Defines a few nice extensions for making working with <see cref="DateTimeOffset"/> more nice
/// </summary>
public static class DateTimeExtensions
{
    /// <summary>
    /// Gets the time from this instant until now (as returned by <see cref="IRebusTime.Now"/>)
    /// </summary>
    public static TimeSpan ElapsedUntilNow(this DateTimeOffset dateTime, IRebusTime rebusTime)
    {
        return rebusTime.Now - dateTime.ToUniversalTime();
    }

    /// <summary>
    /// Serializes this instant with the "O" format, i.e. ISO8601-compliant
    /// </summary>
    public static string ToIso8601DateTimeOffset(this DateTimeOffset dateTimeOffset)
    {
        return dateTimeOffset.ToString("O", CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Parses an ISO8601-compliant string into a proper <see cref="DateTimeOffset"/>
    /// </summary>
    public static DateTimeOffset ToDateTimeOffset(this string iso8601String)
    {
        if (!DateTimeOffset.TryParseExact(iso8601String, "O", CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var result))
        {
            throw new FormatException($"Could not parse '{iso8601String}' as a proper ISO8601-formatted DateTimeOffset!");
        }

        return result;
    }
}