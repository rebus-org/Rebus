using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Rebus.Timeouts;

/// <summary>
/// Abstraction for a mechanism that is capable of storing deferred messages until the time where it's appropriate for it to be delivered.
/// </summary>
public interface ITimeoutManager
{
    /// <summary>
    /// Stores the message with the given headers and body data, delaying it until the specified <paramref name="approximateDueTime"/>
    /// </summary>
    Task Defer(DateTimeOffset approximateDueTime, Dictionary<string, string> headers, byte[] body);

    /// <summary>
    /// Gets due messages as of now, given the approximate due time that they were stored with when <see cref="Defer"/> was called
    /// </summary>
    Task<DueMessagesResult> GetDueMessages();
}