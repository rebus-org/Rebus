using System;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Rebus.Activation;
using Rebus.Config;
using Rebus.Extensions;
using Rebus.Persistence.SqlServer;
using Rebus.Routing.TypeBased;
using Rebus.Tests.Extensions;
using Rebus.Transport.InMem;

namespace Rebus.Tests.Integration
{
    [TestFixture]
    public class TestTypeBasedRouting : FixtureBase
    {
        ManualResetEvent _client1GotTheEvent;
        BuiltinHandlerActivator _client1;
        BuiltinHandlerActivator _publisher;

        protected override void SetUp()
        {
            SqlTestHelper.DropTable("subscriptions");

            var network = new InMemNetwork();
            _publisher = GetEndpoint(network, "publisher", c =>
            {
                c.Subscriptions(s => s.StoreInSqlServer(SqlTestHelper.ConnectionString, "subscriptions"));
                c.Routing(r => r.TypeBased());
            });

            _client1GotTheEvent = new ManualResetEvent(false);
            _client1 = GetEndpoint(network, "client1", c =>
            {
                c.Routing(r => r.TypeBased().Map<SomeKindOfEvent>("publisher"));
            });
            _client1.Handle<SomeKindOfEvent>(async e => _client1GotTheEvent.Set());
        }

        [Test]
        public async Task TypeBasedRoutingAndExtensionMethodsAndEverythingWorksAsItShould()
        {
            await _client1.Bus.Subscribe<SomeKindOfEvent>();

            await Task.Delay(500);

            await _publisher.Bus.Publish(new SomeKindOfEvent());

            _client1GotTheEvent.WaitOrDie(TimeSpan.FromSeconds(2));
        }

        [Test]
        public async Task TypeBasedRoutingAndExtensionMethodsAndEverythingWorksAsItShouldAlsoWhenTypeIsNotInferred()
        {
            await _client1.Bus.Subscribe<SomeKindOfEvent>();

            await Task.Delay(500);

            object someKindOfEvent = new SomeKindOfEvent();

            await _publisher.Bus.Publish(someKindOfEvent);

            _client1GotTheEvent.WaitOrDie(TimeSpan.FromSeconds(2), "Looks like the publish topic was not correctly inferred!");
        }

        class SomeKindOfEvent { }

        BuiltinHandlerActivator GetEndpoint(InMemNetwork network, string queueName, Action<RebusConfigurer> additionalConfiguration = null)
        {
            var activator = Using(new BuiltinHandlerActivator());

            var configurer = Configure.With(activator)
                .Transport(t => t.UseInMemoryTransport(network, queueName));

            if (additionalConfiguration != null)
            {
                additionalConfiguration(configurer);
            }
             
            configurer.Start();

            return activator;
        }
    }
}