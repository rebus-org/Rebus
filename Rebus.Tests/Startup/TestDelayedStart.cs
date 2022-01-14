using System;
using System.Threading.Tasks;
using NUnit.Framework;
using Rebus.Activation;
using Rebus.Bus;
using Rebus.Config;
using Rebus.Routing.TypeBased;
using Rebus.Tests.Contracts;
using Rebus.Tests.Contracts.Utilities;
using Rebus.Transport.InMem;
// ReSharper disable UnusedVariable
#pragma warning disable 1998

namespace Rebus.Tests.Startup;

[TestFixture]
public class TestDelayedStart : FixtureBase
{
    [Test]
    public async Task CanDelayStartingTheBus()
    {
        var network = new InMemNetwork();
        var activator = new BuiltinHandlerActivator();

        Using(activator);

        var messageWasReceived = false;
        var counter = new SharedCounter(1);

        var sender = CreateSender(network);

        activator.Handle<string>(async str =>
        {
            messageWasReceived = true;
            counter.Decrement();
        });

        var starter = Configure.With(activator)
            .Logging(l => l.Console())
            .Transport(t => t.UseInMemoryTransport(network, "whatever dude"))
            .Create();

        await sender.Send("HEJ MED DIG MIN VEN!!");

        await Task.Delay(TimeSpan.FromSeconds(1));

        Assert.That(messageWasReceived, Is.False,
            "Did not expect to have received the message yet, because the bus was not started");

        var bus = starter.Start();

        counter.WaitForResetEvent();

        Assert.That(messageWasReceived, Is.True, "Expected that the message would have been received by now");
    }

    IBus CreateSender(InMemNetwork network)
    {
        var activator = Using(new BuiltinHandlerActivator());

        return Configure.With(activator)
            .Logging(l => l.Console())
            .Transport(t => t.UseInMemoryTransportAsOneWayClient(network))
            .Routing(r => r.TypeBased().Map<string>("whatever dude"))
            .Start();
    }
}