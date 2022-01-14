using System;

namespace Rebus.Time;

/// <summary>
/// Default implementation of <see cref="IRebusTime"/> that returns the system clock time
/// </summary>
public class DefaultRebusTime : IRebusTime
{
    /// <summary>
    /// Gets the current time
    /// </summary>
    public DateTimeOffset Now => DateTimeOffset.Now;
}