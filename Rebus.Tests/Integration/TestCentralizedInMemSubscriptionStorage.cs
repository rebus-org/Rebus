using System;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Rebus.Activation;
using Rebus.Config;
using Rebus.Persistence.InMem;
using Rebus.Tests.Contracts;
using Rebus.Tests.Contracts.Extensions;
using Rebus.Transport.InMem;
#pragma warning disable 1998

namespace Rebus.Tests.Integration;

[TestFixture]
public class TestCentralizedInMemSubscriptionStorage : FixtureBase
{
    InMemNetwork _network;
    InMemorySubscriberStore _subscriberStore;
    BuiltinHandlerActivator _activator;
    IBusStarter _starter;

    protected override void SetUp()
    {
        _network = new InMemNetwork();
        _subscriberStore = new InMemorySubscriberStore();

        _activator = new BuiltinHandlerActivator();

        Using(_activator);

        _starter = Configure.With(_activator)
            .Transport(t => t.UseInMemoryTransport(_network, "endpoint1", registerSubscriptionStorage: false))
            .Subscriptions(s => s.StoreInMemory(_subscriberStore))
            .Create();
    }

    [Test]
    public async Task CanSubscribeEvenThoughWeHaveNotConfiguredAnyEndpointMappings()
    {
        var gotTheString = new ManualResetEvent(false);

        _activator.Handle<string>(async str => gotTheString.Set());

        var bus = _starter.Start();

        await bus.Subscribe<string>();

        await Task.Delay(500);

        await _activator.Bus.Publish("whooo hooo this is a string!!");

        gotTheString.WaitOrDie(TimeSpan.FromSeconds(2));
    }
}