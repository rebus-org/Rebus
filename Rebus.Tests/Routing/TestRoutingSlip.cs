using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Rebus.Activation;
using Rebus.Bus;
using Rebus.Config;
using Rebus.Routing;
using Rebus.Tests.Contracts;
using Rebus.Tests.Contracts.Extensions;
using Rebus.Transport.InMem;
#pragma warning disable 1998

namespace Rebus.Tests.Routing
{
    [TestFixture]
    public class TestRoutingSlip : FixtureBase
    {
        readonly InMemNetwork _network = new InMemNetwork();

        protected override void SetUp()
        {
            _network.Reset();
        }

        [Test]
        public async Task CanRouteMessageAsExpected()
        {
            var a = StartBus("endpoint-a");
            var b = StartBus("endpoint-b");
            var c = StartBus("endpoint-c");

            var routingSlipWasReturnedToSender = new ManualResetEvent(false);

            var initiator = StartBus("initiator", message => routingSlipWasReturnedToSender.Set());

            var itinerary = new Itinerary("endpoint-a", "endpoint-b", "endpoint-c")
                .ReturnToSender();

            await initiator.Bus.Advanced.Routing.SendRoutingSlip(itinerary, new SomeMessage("hello there!"));

            routingSlipWasReturnedToSender.WaitOrDie(TimeSpan.FromSeconds(3));

            Assert.That(a.Events, Contains.Item(@"Handled string ""hello there!"" in endpoint-a"));
            Assert.That(b.Events, Contains.Item(@"Handled string ""hello there!"" in endpoint-b"));
            Assert.That(c.Events, Contains.Item(@"Handled string ""hello there!"" in endpoint-c"));
        }

        class SomeMessage
        {
            public SomeMessage(string text)
            {
                Text = text;
            }

            public string Text { get; }
        }

        RoutingSlipDestination StartBus(string queueName, Action<SomeMessage> messageHandler = null)
        {
            var activator = new BuiltinHandlerActivator();
            var events = new ConcurrentQueue<string>();

            Using(activator);

            if (messageHandler == null)
            {
                activator.Handle<SomeMessage>(async message =>
                {
                    var text = $@"Handled string ""{message.Text}"" in {queueName}";
                    Console.WriteLine(text);
                    events.Enqueue(text);
                });
            }
            else
            {
                activator.Handle<SomeMessage>(async message => messageHandler(message));
            }

            Configure.With(activator)
                .Transport(t => t.UseInMemoryTransport(_network, queueName))
                .Start();

            return new RoutingSlipDestination(activator.Bus, activator, events);
        }

        class RoutingSlipDestination
        {
            public IBus Bus { get; }
            public BuiltinHandlerActivator Activator { get; }
            public ConcurrentQueue<string> Events { get; }

            public RoutingSlipDestination(IBus bus, BuiltinHandlerActivator activator, ConcurrentQueue<string> events)
            {
                Bus = bus;
                Activator = activator;
                Events = events;
            }
        }

    }
}