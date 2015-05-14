using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Rebus.Activation;
using Rebus.Bus;
using Rebus.Config;
using Rebus.Tests.Extensions;
using Rebus.Transport.InMem;

namespace Rebus.Tests.Integration
{
    [TestFixture]
    public class TestPolymorphicDispatch : FixtureBase
    {
        BuiltinHandlerActivator _handlerActivator;
        IBus _bus;

        protected override void SetUp()
        {
            _handlerActivator = new BuiltinHandlerActivator();

            _bus = Configure.With(_handlerActivator)
                .Transport(t => t.UseInMemoryTransport(new InMemNetwork(), "input"))
                .Options(o =>
                {
                    o.SetNumberOfWorkers(1);
                    o.SetMaxParallelism(1);
                })
                .Start();

            Using(_bus);
        }

        [Test]
        public async Task ItWorksInSimpleScenario()
        {
            var events = new ConcurrentQueue<string>();
            var gotMessage = new AutoResetEvent(false);

            _handlerActivator.Handle<BaseMessage>(async msg =>
            {
                events.Enqueue(string.Format("Got {0} with {1}", msg.GetType().Name, msg.Payload));

                gotMessage.Set();
            });

            await _bus.SendLocal(new SpecializationA { Payload = "a" });
            await _bus.SendLocal(new SpecializationB { Payload = "b" });

            gotMessage.WaitOrDie(TimeSpan.FromSeconds(1), "Did not get the first message");
            gotMessage.WaitOrDie(TimeSpan.FromSeconds(1), "Did not get the second message");

            Assert.That(events.ToArray(), Is.EqualTo(new[]
            {
                "Got SpecializationA with a",
                "Got SpecializationB with b",
            }));
        }

        [Test]
        public async Task CanHandleObject()
        {
            var events = new ConcurrentQueue<string>();
            var gotMessage = new AutoResetEvent(false);

            _handlerActivator.Handle<object>(async msg =>
            {
                events.Enqueue(string.Format("Got {0}", msg.GetType().Name));
                gotMessage.Set();
            });

            await _bus.SendLocal("hej med dig");

            gotMessage.WaitOrDie(TimeSpan.FromSeconds(1));

            Assert.That(events.ToArray(), Is.EqualTo(new[]
            {
                "Got String",
            }));
        }

        [Test]
        public async Task CanHandleInterface()
        {
            var events = new ConcurrentQueue<string>();
            var gotMessage = new AutoResetEvent(false);

            _handlerActivator.Handle<IMessage>(async msg =>
            {
                events.Enqueue(string.Format("Got {0}", msg.GetType().Name));
                gotMessage.Set();
            });

            await _bus.SendLocal(new ImplementorOfInterface());

            gotMessage.WaitOrDie(TimeSpan.FromSeconds(1));

            Assert.That(events.ToArray(), Is.EqualTo(new[]
            {
                "Got ImplementorOfInterface",
            }));
        }

        abstract class BaseMessage
        {
            public string Payload { get; set; }
        }

        class SpecializationA : BaseMessage { }
        class SpecializationB : BaseMessage { }

        interface IMessage { }

        class ImplementorOfInterface : IMessage { }
    }
}