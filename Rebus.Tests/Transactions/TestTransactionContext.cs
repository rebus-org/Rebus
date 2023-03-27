using System.Collections.Concurrent;
using System.Threading.Tasks;
using NUnit.Framework;
using Rebus.Activation;
using Rebus.Bus;
using Rebus.Config;
using Rebus.Tests.Contracts;
using Rebus.Transport;
using Rebus.Transport.InMem;
// ReSharper disable ConvertToUsingDeclaration
#pragma warning disable IDE0063
#pragma warning disable IDE0063
#pragma warning disable CS1998

namespace Rebus.Tests.Transactions;

[TestFixture]
public class TestTransactionContext : FixtureBase
{
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