using System;

namespace Rebus.Time;

/// <summary>
/// Represents time
/// </summary>
public interface IRebusTime
{
    /// <summary>
    /// Gets the current time
    /// </summary>
    DateTimeOffset Now { get; }
}