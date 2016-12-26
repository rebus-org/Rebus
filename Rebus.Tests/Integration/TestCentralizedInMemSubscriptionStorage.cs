using System;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Rebus.Activation;
using Rebus.Config;
using Rebus.Persistence.InMem;
using Rebus.Tests.Contracts;
using Rebus.Tests.Contracts.Extensions;
using Rebus.Tests.Extensions;
using Rebus.Transport.InMem;
#pragma warning disable 1998

namespace Rebus.Tests.Integration
{
    [TestFixture]
    public class TestCentralizedInMemSubscriptionStorage : FixtureBase
    {
        InMemNetwork _network;
        InMemorySubscriberStore _subscriberStore;
        BuiltinHandlerActivator _activator;

        protected override void SetUp()
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

        [Test]
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