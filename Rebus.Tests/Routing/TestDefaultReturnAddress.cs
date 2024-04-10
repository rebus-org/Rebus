using System;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Rebus.Activation;
using Rebus.Config;
using Rebus.Messages;
using Rebus.Routing.TypeBased;
using Rebus.Tests.Contracts;
using Rebus.Tests.Contracts.Extensions;
using Rebus.Transport.InMem;
// ReSharper disable AccessToDisposedClosure
#pragma warning disable 1998

namespace Rebus.Tests.Routing;

[TestFixture]
public class TestDefaultReturnAddress : FixtureBase
{
    [Test]
    public async Task AssignsDefaultReturnAddressOnSentMessage()
    {
        var network = new InMemNetwork();

        var sender = Using(new BuiltinHandlerActivator());

        Configure.With(sender)
            .Transport(t => t.UseInMemoryTransport(network, "queue-a"))
            .Routing(r => r.TypeBased().Map<string>("queue-b"))
            .Options(o => o.SetDefaultReturnAddress("a totally different queue name"))
            .Start();

        var receiver = Using(new BuiltinHandlerActivator());

        var returnAddress = "";

        using var done = new ManualResetEvent(false);

        receiver.Handle<string>(async (bus, context, message) =>
        {
            returnAddress = context.Headers[Headers.ReturnAddress];
            done.Set();
        });

        Configure.With(receiver)
            .Transport(t => t.UseInMemoryTransport(network, "queue-b"))
            .Start();

        await sender.Bus.Send("HEJ MED DIG MIN VEEEEN!");

        done.WaitOrDie(TimeSpan.FromSeconds(2));

        Assert.That(returnAddress, Is.EqualTo("a totally different queue name"), "Expected a totally different queue name here");
    }

    [Test]
    public async Task AssignsDefaultReturnAddressOnSentMessage_OneWayClient()
    {
        var network = new InMemNetwork();

        using var client = Configure.OneWayClient()
            .Transport(t => t.UseInMemoryTransportAsOneWayClient(network))
            .Routing(r => r.TypeBased().Map<string>("queue-b"))
            .Options(o => o.SetDefaultReturnAddress("a totally different queue name"))
            .Start();

        var receiver = Using(new BuiltinHandlerActivator());

        var returnAddress = "";

        using var done = new ManualResetEvent(false);

        receiver.Handle<string>(async (bus, context, message) =>
        {
            returnAddress = context.Headers[Headers.ReturnAddress];
            done.Set();
        });

        Configure.With(receiver)
            .Transport(t => t.UseInMemoryTransport(network, "queue-b"))
            .Start();

        await client.Send("HEJ MED DIG MIN VEEEEN!");

        done.WaitOrDie(TimeSpan.FromSeconds(2));

        Assert.That(returnAddress, Is.EqualTo("a totally different queue name"), "Expected a totally different queue name here");
    }
}