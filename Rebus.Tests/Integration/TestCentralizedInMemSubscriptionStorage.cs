using System;
using System.Threading;
using System.Threading.Tasks;
using Rebus.Activation;
using Rebus.Config;
using Rebus.Persistence.InMem;
using Rebus.Tests.Contracts;
using Rebus.Tests.Contracts.Extensions;
using Rebus.Transport.InMem;
using Xunit;

#pragma warning disable 1998

namespace Rebus.Tests.Integration
{
    public class TestCentralizedInMemSubscriptionStorage : FixtureBase
    {
        readonly InMemNetwork _network;
        readonly InMemorySubscriberStore _subscriberStore;
        readonly BuiltinHandlerActivator _activator;

        public TestCentralizedInMemSubscriptionStorage()
        {
            _network = new InMemNetwork();
            _subscriberStore = new InMemorySubscriberStore();

            _activator = new BuiltinHandlerActivator();

            Using(_activator);

            Configure.With(_activator)
                .Transport(t => t.UseInMemoryTransport(_network, "endpoint1"))
                .Subscriptions(s => s.StoreInMemory(_subscriberStore))
                .Start();
        }

        [Fact]
        public async Task CanSubscribeEvenThoughWeHaveNotConfiguredAnyEndpointMappings()
        {
            var gotTheString = new ManualResetEvent(false);

            _activator.Handle<string>(async str => gotTheString.Set());

            await _activator.Bus.Subscribe<string>();

            await Task.Delay(500);

            await _activator.Bus.Publish("whooo hooo this is a string!!");

            gotTheString.WaitOrDie(TimeSpan.FromSeconds(2));
        }
    }
}