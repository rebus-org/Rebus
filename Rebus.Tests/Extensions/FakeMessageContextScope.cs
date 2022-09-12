using System;
using System.Collections.Generic;
using Rebus.Messages;
using Rebus.Pipeline;
using Rebus.Transport;

namespace Rebus.Tests.Extensions;

class FakeMessageContextScope : IDisposable
{
    readonly RebusTransactionScope _rebusTransactionScope = new();

    public FakeMessageContextScope()
    {
        TransactionContext.Items[StepContext.StepContextKey] = new IncomingStepContext(new TransportMessage(new Dictionary<string, string>(), new byte[] { 1, 2, 3 }), TransactionContext);
    }

    public ITransactionContext TransactionContext => _rebusTransactionScope.TransactionContext;

    public void Dispose()
    {
        TransactionContext.GetOrThrow<IncomingStepContext>(StepContext.StepContextKey).Dispose();

        _rebusTransactionScope.Dispose();
    }
}