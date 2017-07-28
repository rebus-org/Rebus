using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Rebus.Timeouts;

namespace Rebus.Persistence.Throwing
{
    class DisabledTimeoutManager : ITimeoutManager
    {
        public Task Defer(DateTimeOffset approximateDueTime, Dictionary<string, string> headers, byte[] body)
        {
            throw new NotImplementedException();
        }

        public Task<DueMessagesResult> GetDueMessages()
        {
            throw new NotImplementedException();
        }
    }
}