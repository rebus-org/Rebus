using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Rebus.Activation;
using Rebus.Bus;
using Rebus.Config;
using Rebus.Exceptions;
using Rebus.Retry.Simple;
using Rebus.Tests.Contracts;
using Rebus.Tests.Contracts.Extensions;
using Rebus.Transport;
using Rebus.Transport.InMem;
// ReSharper disable ConvertToUsingDeclaration
// ReSharper disable AccessToDisposedClosure
#pragma warning disable IDE0063
#pragma warning disable IDE0063
#pragma warning disable CS1998

namespace Rebus.Tests.Transactions;

[TestFixture]
public class TestTransactionContext : FixtureBase
{
    [Test]
    [Description("Ensures that the callbacks raised from the transaction context are raised at points in time where the ambient transaction context is available")]
    public void EnsureTransactionContextIsAvailableInItsOwnCallbacks_RebusTransactionScope()
    {
        bool FoundAmbientTransactionContext() => AmbientTransactionContext.Current != null;

        var committedCallbackWasCalled = false;
        var foundTransactionContextInCommittedCallback = false;

        var rollbackCallbackWasCalled = false;
        var foundTransactionContextInRollbackCallback = false;

        var disposedCallbackWasCalled = false;
        var foundTransactionContextInDisposedCallback = false;

        RebusTransactionScope CreateRebusTransactionScope()
        {
            var scope = new RebusTransactionScope();

            scope.TransactionContext.OnCommit(async transactionContext =>
            {
                committedCallbackWasCalled = true;
                foundTransactionContextInCommittedCallback = FoundAmbientTransactionContext()
                                                             && transactionContext != null;
            });

            scope.TransactionContext.OnRollback(async transactionContext =>
            {
                rollbackCallbackWasCalled = true;
                foundTransactionContextInRollbackCallback = FoundAmbientTransactionContext()
                                                            && transactionContext != null;
            });

            scope.TransactionContext.OnDisposed(async transactionContext =>
            {
                disposedCallbackWasCalled = true;
                foundTransactionContextInDisposedCallback = FoundAmbientTransactionContext()
                                                            && transactionContext != null;
            });

            return scope;
        }

        using (var scope = CreateRebusTransactionScope())
        {
            scope.Complete();
        }

        using (CreateRebusTransactionScope())
        {
        }

        Assert.That(disposedCallbackWasCalled, Is.True, $"{nameof(ITransactionContext.OnDisposed)} callback was not called");
        Assert.That(committedCallbackWasCalled, Is.True, $"{nameof(ITransactionContext.OnCommit)} callback was not called");
        Assert.That(rollbackCallbackWasCalled, Is.True, $"{nameof(ITransactionContext.OnRollback)} callback was not called");

        Assert.That(foundTransactionContextInDisposedCallback, Is.True, $"Did not find the expected tx context in {nameof(ITransactionContext.OnDisposed)} callback");
        Assert.That(foundTransactionContextInCommittedCallback, Is.True, $"Did not find the expected tx context in {nameof(ITransactionContext.OnCommit)} callback");
        Assert.That(foundTransactionContextInRollbackCallback, Is.True, $"Did not find the expected tx context in {nameof(ITransactionContext.OnRollback)} callback");
    }

    [Test]
    [Description("Ensures that the callbacks raised from the transaction context are raised at points in time where the ambient transaction context is available")]
    public async Task EnsureTransactionContextIsAvailableInItsOwnCallbacks_Handler()
    {
        static bool FoundAmbientTransactionContext() => AmbientTransactionContext.Current != null;

        using var activator = new BuiltinHandlerActivator();

        using var committedCallbackCalled = new ManualResetEvent(initialState: false);
        using var rollbackCallbackCalled = new ManualResetEvent(initialState: false);
        using var disposedCallbackCalled = new ManualResetEvent(initialState: false);

        var foundTransactionContextInCommittedCallback = false;
        var foundTransactionContextInRollbackCallback = false;
        var foundTransactionContextInDisposedCallback = false;

        activator.Handle<string>(async (_, context, message) =>
        {
            // this callback can access the transaction context via the ambient tx accessor or via the passed-in tx context
            context.TransactionContext.OnCommit(async transactionContext =>
            {
                foundTransactionContextInCommittedCallback = FoundAmbientTransactionContext()
                                                             && transactionContext != null;
                committedCallbackCalled.Set();
            });

            // this callback can access the transaction context via the ambient tx accessor or via the passed-in tx context
            context.TransactionContext.OnRollback(async transactionContext =>
            {
                foundTransactionContextInRollbackCallback = FoundAmbientTransactionContext()
                                                            && transactionContext != null;
                rollbackCallbackCalled.Set();
            });

            // this check is just here to show how the transaction context must be accessed from the disposed callback, because
            // the ambient transaction context is no longer available at this point
            context.TransactionContext.OnDisposed(transactionContext =>
            {
                foundTransactionContextInDisposedCallback = transactionContext != null;
                disposedCallbackCalled.Set();
            });

            if (message == "throw!")
            {
                throw new RebusApplicationException("thrown!");
            }
        });

        Configure.With(activator)
            .Transport(t => t.UseInMemoryTransport(new InMemNetwork(), "whatever"))
            .Options(o => o.RetryStrategy(maxDeliveryAttempts: 1))
            .Start();

        await activator.Bus.SendLocal("hej du");
        await activator.Bus.SendLocal("throw!");

        committedCallbackCalled.WaitOrDie(TimeSpan.FromSeconds(2), errorMessage: $"{nameof(TransactionContext.OnCommit)} callback was not called");
        rollbackCallbackCalled.WaitOrDie(TimeSpan.FromSeconds(2), errorMessage: $"{nameof(TransactionContext.OnRollback)} callback was not called");
        disposedCallbackCalled.WaitOrDie(TimeSpan.FromSeconds(2), errorMessage: $"{nameof(TransactionContext.OnDisposed)} callback was not called");

        Assert.That(foundTransactionContextInCommittedCallback, Is.True,
            $"Expected to find an ambient transaction context in the {nameof(ITransactionContext.OnCommit)} callback from the transaction context");
        Assert.That(foundTransactionContextInRollbackCallback, Is.True,
            $"Expected to find an ambient transaction context in the {nameof(ITransactionContext.OnRollback)} callback from the transaction context");
        Assert.That(foundTransactionContextInDisposedCallback, Is.True,
            $"Expected to find an ambient transaction context in the {nameof(ITransactionContext.OnDisposed)} callback from the transaction context");
    }

    [Test]
    public async Task SunshineScenario()
    {
        var events = new ConcurrentQueue<string>();

        using (var context = new TransactionContextWithOwningBus(GetBus()))
        {
            context.OnCommit(async _ => events.Enqueue("commit"));
            context.OnAck(async _ => events.Enqueue("ack"));
            context.OnRollback(async _ => events.Enqueue("rollback"));
            context.OnNack(async _ => events.Enqueue("nack"));
            context.OnDisposed(async _ => events.Enqueue("dispose"));

            context.SetResult(commit: true, ack: true);

            await context.Complete();
        }

        Assert.That(events.ToArray(), Is.EqualTo(new[] { "commit", "ack", "dispose" }));
    }

    [Test]
    public async Task AbortAndRetryScenario()
    {
        var events = new ConcurrentQueue<string>();

        using (var context = new TransactionContextWithOwningBus(GetBus()))
        {
            context.OnCommit(async _ => events.Enqueue("commit"));
            context.OnAck(async _ => events.Enqueue("ack"));
            context.OnRollback(async _ => events.Enqueue("rollback"));
            context.OnNack(async _ => events.Enqueue("nack"));
            context.OnDisposed(async _ => events.Enqueue("dispose"));

            context.SetResult(commit: false, ack: false);

            await context.Complete();
        }

        Assert.That(events.ToArray(), Is.EqualTo(new[] { "rollback", "nack", "dispose" }));
    }

    [Test]
    public async Task AbortAndForwardToDeadletterQueueScenario()
    {
        var events = new ConcurrentQueue<string>();

        using (var context = new TransactionContextWithOwningBus(GetBus()))
        {
            context.OnCommit(async _ => events.Enqueue("commit"));
            context.OnAck(async _ => events.Enqueue("ack"));
            context.OnRollback(async _ => events.Enqueue("rollback"));
            context.OnNack(async _ => events.Enqueue("nack"));
            context.OnDisposed(async _ => events.Enqueue("dispose"));

            context.SetResult(commit: false, ack: true);

            await context.Complete();
        }

        Assert.That(events.ToArray(), Is.EqualTo(new[] { "rollback", "ack", "dispose" }));
    }

    RebusBus GetBus() => (RebusBus)Configure.With(Using(new BuiltinHandlerActivator()))
        .Transport(t => t.UseInMemoryTransport(new(), "_"))
        .Start();
}