using System;
using System.Threading.Tasks;
using NUnit.Framework;
using Rebus.Activation;
using Rebus.Config;
using Rebus.Exceptions;
using Rebus.Retry.Simple;
using Rebus.Tests.Contracts;
using Rebus.Transport;
using Rebus.Transport.InMem;
#pragma warning disable 1998

namespace Rebus.Tests.Transactions;

[TestFixture]
[Description("Ensures that the callbacks raised from the transaction context are raised at points in time where the ambient transaction context is available")]
public class TransactionContextTests : FixtureBase
{
    [Test]
    public async Task EnsureTransactionContextIsAvailableInItsOwnCallbacks()
    {
        bool FoundTransactionContext() => AmbientTransactionContext.Current != null;

        var activator = Using(new BuiltinHandlerActivator());

        var foundTransactionContextInCommittedCallback = false;
        var foundTransactionContextInAbortedCallback = false;
        var foundTransactionContextInDisposedCallback = false;

        activator.Handle<string>(async (bus, context, message) =>
        {
            // this callback can access the transaction context via the ambient tx accessor or via the passed-in tx context
            context.TransactionContext.OnCommitted(async transactionContext =>
            {
                foundTransactionContextInCommittedCallback = FoundTransactionContext()
                                                             && transactionContext != null;
            });

            // this callback can access the transaction context via the ambient tx accessor or via the passed-in tx context
            context.TransactionContext.OnAborted(transactionContext =>
            {
                foundTransactionContextInAbortedCallback = FoundTransactionContext()
                                                           && transactionContext != null;
            });

            // this check is just here to show how the transaction context must be access from the disposed callback, because
            // the ambient transaction context is no longer available at this point
            context.TransactionContext.OnDisposed(transactionContext => foundTransactionContextInDisposedCallback = transactionContext != null);

            if (message == "throw!") throw new RebusApplicationException("thrown!");
        });

        Configure.With(activator)
            .Transport(t => t.UseInMemoryTransport(new InMemNetwork(), "whatever"))
            .Options(o => o.SimpleRetryStrategy(maxDeliveryAttempts: 1))
            .Start();

        await activator.Bus.SendLocal("hej du");
        await activator.Bus.SendLocal("throw!");

        await Task.Delay(TimeSpan.FromSeconds(1));

        Assert.That(foundTransactionContextInCommittedCallback, Is.True,
            "Expected to find an ambient transaction context in the OnCommitted callback from the transaction context");
        Assert.That(foundTransactionContextInAbortedCallback, Is.True,
            "Expected to find an ambient transaction context in the OnAborted callback from the transaction context");
        Assert.That(foundTransactionContextInDisposedCallback, Is.True,
            "Expected to find an ambient transaction context in the OnDisposed callback from the transaction context");
    }
}