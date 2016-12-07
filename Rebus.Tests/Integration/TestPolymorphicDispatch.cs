using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Rebus.Activation;
using Rebus.Bus;
using Rebus.Config;
using Rebus.Tests.Contracts;
using Rebus.Tests.Contracts.Extensions;
using Rebus.Transport.InMem;
using Xunit;

#pragma warning disable 1998

namespace Rebus.Tests.Integration
{
    public class TestPolymorphicDispatch : FixtureBase
    {
        static readonly TimeSpan BlockingWaitTimeout = TimeSpan.FromSeconds(5);
        readonly BuiltinHandlerActivator _handlerActivator;
        IBus _bus;

        public TestPolymorphicDispatch()
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

        [Fact]
        public async Task ItWorksInSimpleScenario()
        {
            var events = new ConcurrentQueue<string>();
            var gotMessage = new AutoResetEvent(false);

            _handlerActivator.Handle<BaseMessage>(async msg =>
            {
                events.Enqueue($"Got {msg.GetType().Name} with {msg.Payload}");

                gotMessage.Set();
            });

            await _bus.SendLocal(new SpecializationA { Payload = "a" });
            await _bus.SendLocal(new SpecializationB { Payload = "b" });

            gotMessage.WaitOrDie(BlockingWaitTimeout, "Did not get the first message");
            gotMessage.WaitOrDie(BlockingWaitTimeout, "Did not get the second message");

            Assert.Equal(new[] {"Got SpecializationA with a","Got SpecializationB with b",},
                events.ToArray());
        }

        [Fact]
        public async Task CanHandleObject()
        {
            var events = new ConcurrentQueue<string>();
            var gotMessage = new AutoResetEvent(false);

            _handlerActivator.Handle<object>(async msg =>
            {
                events.Enqueue($"Got {msg.GetType().Name}");
                gotMessage.Set();
            });

            await _bus.SendLocal("hej med dig");

            gotMessage.WaitOrDie(BlockingWaitTimeout);

            Assert.Equal(new[] { "Got String", }, events.ToArray());
        }

        [Fact]
        public async Task CanHandleInterface()
        {
            var events = new ConcurrentQueue<string>();
            var gotMessage = new AutoResetEvent(false);

            _handlerActivator.Handle<IMessage>(async msg =>
            {
                events.Enqueue($"Got {msg.GetType().Name}");
                gotMessage.Set();
            });

            await _bus.SendLocal(new ImplementorOfInterface());

            gotMessage.WaitOrDie(BlockingWaitTimeout);

            Assert.Equal(new[] { "Got ImplementorOfInterface",}, events.ToArray());
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