using System;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Rebus.Activation;
using Rebus.Config;
using Rebus.Exceptions;
using Rebus.Retry.Simple;
using Rebus.Routing.TypeBased;
using Rebus.Tests.Contracts;
using Rebus.Tests.Contracts.Extensions;
using Rebus.Transport.InMem;

// ReSharper disable AccessToDisposedClosure
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously

namespace Rebus.Tests.Bugs;

[TestFixture]
[Description("This test simply verifies that FirstLevelMessageTransactionIsNotCommittedWhenItFailsEvenThoughSecondLevelRetryKicksInAndSucceeds")]
public class MessageTransactionsAndVariousLevelsOfRetries : FixtureBase
{
    [Test]
    public async Task FirstLevelMessageTransactionIsNotCommittedWhenItFailsEvenThoughSecondLevelRetryKicksInAndSucceeds()
    {
        using var activator = new BuiltinHandlerActivator();
        using var done = new ManualResetEvent(initialState: false);

        var network = new InMemNetwork();

        network.CreateQueue("other-queue");

        activator.Handle<Mensajito>(async (bus, _) =>
        {
            // send another message
            await bus.Send(new AnotherMessage());

            // throw exception
            throw new FailFastException("💀");
        });

        activator.Handle<IFailed<Mensajito>>(async _ => done.Set());

        var bus = Configure.With(activator)
            .Transport(t => t.UseInMemoryTransport(network, "whatever"))
            .Options(o => o.RetryStrategy(secondLevelRetriesEnabled: true))
            .Routing(r => r.TypeBased().Map<AnotherMessage>("other-queue"))
            .Start();

        await bus.SendLocal(new Mensajito());

        done.WaitOrDie(TimeSpan.FromSeconds(3));

        await Task.Delay(millisecondsDelay: 100);

        Assert.That(network.Count("other-queue"), Is.Zero, 
            "Expected 'other-queue' to be empty, because the outgoing AnotherMessage should not have been sent");
    }

    record Mensajito;

    record AnotherMessage;
}