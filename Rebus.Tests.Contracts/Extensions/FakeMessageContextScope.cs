using System;
using System.Collections.Generic;
using Rebus.Messages;
using Rebus.Pipeline;
using Rebus.Transport;

namespace Rebus.Tests.Contracts.Extensions;

class FakeMessageContextScope : IDisposable
{
    readonly RebusTransactionScope _rebusTransactionScope = new RebusTransactionScope();

    public FakeMessageContextScope()
    {
        TransactionContext.Items[StepContext.StepContextKey] = new IncomingStepContext(new TransportMessage(new Dictionary<string, string>(), new byte[] { 1, 2, 3 }), TransactionContext);
    }

    public ITransactionContext TransactionContext => _rebusTransactionScope.TransactionContext;

    public void Dispose() => _rebusTransactionScope.Dispose();
}