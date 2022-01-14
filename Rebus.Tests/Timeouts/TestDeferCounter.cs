using System;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Rebus.Activation;
using Rebus.Config;
using Rebus.Extensions;
using Rebus.Messages;
using Rebus.Persistence.InMem;
using Rebus.Routing.TypeBased;
using Rebus.Tests.Contracts;
using Rebus.Tests.Contracts.Extensions;
using Rebus.Transport.InMem;
// ReSharper disable ArgumentsStyleStringLiteral
// ReSharper disable ArgumentsStyleOther

namespace Rebus.Tests.Timeouts;

[TestFixture]
public class TestDeferCounter : FixtureBase
{
    const string DestinationAddress = "defer-recipient";
        
    InMemNetwork _network;
    BuiltinHandlerActivator _activator;
    IBusStarter _starter;

    protected override void SetUp()
    {
        _network = new InMemNetwork();

        _network.CreateQueue(DestinationAddress);
        _activator = new BuiltinHandlerActivator();

        Using(_activator);

        _starter = Configure.With(_activator)
            .Transport(t => t.UseInMemoryTransport(_network, "test-queue"))
            .Routing(r => r.TypeBased().Map<string>(DestinationAddress))
            .Timeouts(t => t.StoreInMemory())
            .Create();
    }

    [Test]
    public async Task DeferredMessageHasDeferCountOf1()
    {
        var bus = _starter.Start();
            
        await bus.Defer(TimeSpan.FromSeconds(0.001), "HEJ MED DIG MIN VEN");

        var message = await _network.WaitForNextMessageFrom(DestinationAddress);
        var headers = message.Headers;

        Assert.That(headers.Keys, Contains.Item("rbs2-defer-count"));
        Assert.That(int.Parse(headers["rbs2-defer-count"]), Is.EqualTo(1));
    }

    [Test]
    public async Task DeferCountIsIncremented()
    {
        var done = Using(new ManualResetEvent(false));
        var dispatchCount = 0;

        const int howManyTimes = 3;
            
        _activator.Handle<string>(async (bus, context, str) =>
        {
            dispatchCount++;

            Console.WriteLine($"Message has been dispatched {dispatchCount} times now!");
                
            var headers = context.Headers;
            var count = int.Parse(headers.GetValue(Headers.DeferCount));

            if (count == howManyTimes)
            {
                done.Set();
                return;
            }

            await bus.Advanced.TransportMessage.Defer(TimeSpan.FromSeconds(0.001));
        });
            
        var deferBus = _starter.Start();

        await deferBus.DeferLocal(TimeSpan.FromSeconds(0.001), "HEJ MED DIG MIN VEN");

        done.WaitOrDie(TimeSpan.FromSeconds(7), 
            errorMessage: $"Message was not dispatched {howManyTimes} times within 7s timeout");

        Assert.That(dispatchCount, Is.EqualTo(howManyTimes));
    }
}