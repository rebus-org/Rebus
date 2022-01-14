using System;

namespace Rebus.DataBus;

/// <summary>
/// Represents a time range criterion
/// </summary>
public class TimeRange
{
    /// <summary>
    /// Gets the lower timestamp
    /// </summary>
    public DateTimeOffset? From { get; }

    /// <summary>
    /// Gets the upper timestamp
    /// </summary>
    public DateTimeOffset? To { get; }

    /// <summary>
    /// Creates the criterion. Both arguments are optional
    /// </summary>
    public TimeRange(DateTimeOffset? from = null, DateTimeOffset? to = null)
    {
        From = from;
        To = to;
    }

    /// <summary>
    /// Renders the time range
    /// </summary>
    public override string ToString() => $"{From} - {To}";
}