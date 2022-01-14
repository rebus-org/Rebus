using System;
using System.Collections.Generic;
using System.Threading.Tasks;
#pragma warning disable 1998

namespace Rebus.Timeouts;

class ThrowingTimeoutManager : ITimeoutManager
{
    public async Task Defer(DateTimeOffset approximateDueTime, Dictionary<string, string> headers, byte[] body)
    {
        throw new InvalidOperationException("Cannot DEFER message with the throwing timeout manager! This timeout manager is installed when deferred messages are to be sent to an external timeout manager!");
    }

    public async Task<DueMessagesResult> GetDueMessages()
    {
        throw new InvalidOperationException("Cannot GET DUE MESSAGES with the throwing timeout manager! This timeout manager is installed when deferred messages are to be sent to an external timeout manager!");
    }
}