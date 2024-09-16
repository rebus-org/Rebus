using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Rebus.Timeouts;

namespace Rebus.Persistence.Throwing;

sealed class DisabledTimeoutManager : ITimeoutManager
{
    public Task Defer(DateTimeOffset approximateDueTime, Dictionary<string, string> headers, byte[] body) => throw GetException();

    public Task<DueMessagesResult> GetDueMessages() => throw GetException();

    static InvalidOperationException GetException() => new(@"A timeout manager has not been configured. Please configure a timeout manager with the .Timeouts(...) configurer, e.g. like so:

Configure.With(..)
    .(...)
    .Timeouts(t => t.StoreInMemory())
    .(...)

in order to save deferred messages in memory, or something like 

Configure.With(..)
    .(...)
    .Timeouts(t => t.StoreSqlServer(...))
    .(...)

if you have imported the Rebus.SqlServer package and want to store deferred messages in SQL Server, or something like

Configure.With(..)
    .(...)
    .Timeouts(t => t.UseExternalTimeoutManager(""another-queue""))
    .(...)

if you want to send deferred messages to another endpoint (which then of course needs to have a timeout manager configured too)
");
}