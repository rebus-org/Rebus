using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using NUnit.Framework;
using Rebus.Activation;
using Rebus.Bus;
using Rebus.Config;
using Rebus.Logging;
using Rebus.Retry.Simple;
using Rebus.Tests;
using Rebus.Transport.InMem;
#pragma warning disable 1998

namespace Rebus.UnitOfWork.Tests
{
    [TestFixture]
    public class TestUnitOfWork : FixtureBase
    {
        const string UowQueueName = "uow-test";
        const string OtherQueueName = "uow-test-recipient";

        ConcurrentQueue<string> _events;
        BuiltinHandlerActivator _uowActivator;
        BuiltinHandlerActivator _otherActivator;
        IBus _uowBus;

        protected override void SetUp()
        {
            var network = new InMemNetwork();

            _events = new ConcurrentQueue<string>();
            _uowActivator = new BuiltinHandlerActivator();
            _otherActivator = new BuiltinHandlerActivator();

            Using(_uowActivator);
            Using(_otherActivator);

            _uowBus = Configure.With(_uowActivator)
                .Logging(l => l.Console(LogLevel.Warn))
                .Transport(t => t.UseInMemoryTransport(network, UowQueueName))
                .Options(o =>
                {
                    o.EnableUnitOfWork(() => _events,
                        commitAction: e => RegisterEvent("uow committed"),
                        rollbackAction: e => RegisterEvent("uow rolled back"),
                        cleanupAction: e => RegisterEvent("uow cleaned up"));

                    o.SimpleRetryStrategy(maxDeliveryAttempts: 1);

                    //o.LogPipeline(true);
                })
                .Start();

            Configure.With(_otherActivator)
                .Logging(l => l.Console(LogLevel.Warn))
                .Transport(t => t.UseInMemoryTransport(network, OtherQueueName))
                .Start();
        }

        [Test]
        public async Task CommitsBeforeSendingMessages()
        {
            var counter = new SharedCounter(1);

            _otherActivator.Handle<string>(async str =>
            {
                RegisterEvent("message sent from uow-enabled endpoint was handled");
            });

            _uowActivator.Handle<string>(async (bus, str) =>
            {
                RegisterEvent("uow-message handled");

                await bus.Advanced.Routing.Send(OtherQueueName, "woohooo!!!");

                counter.Decrement();
            });

            RegisterEvent("message sent");

            await _uowBus.SendLocal("hej med dig min veeeeeen!");

            counter.WaitForResetEvent();

            await Task.Delay(1000);

            var events = _events.ToArray();

            var expectedEvents = new[]
            {
                "message sent",
                "uow-message handled",
                "uow committed",
                "uow cleaned up",
                "message sent from uow-enabled endpoint was handled"
            };

            Assert.That(events, Is.EqualTo(expectedEvents));
        }

        [Test]
        public async Task OutgoingMessagesAreNotSentWhenRollingBack()
        {
            _otherActivator.Handle<string>(async str =>
            {
                RegisterEvent("message sent from uow-enabled endpoint was handled");
            });

            _uowActivator.Handle<string>(async (bus, str) =>
            {
                RegisterEvent("uow-message handled");

                await bus.Advanced.Routing.Send(OtherQueueName, "woohooo!!!");

                throw new InvalidOperationException("bummer, dude!");
            });

            RegisterEvent("message sent");

            await _uowBus.SendLocal("hej med dig min veeeeeen!");

            await Task.Delay(2000);

            var events = _events.ToArray();

            var expectedEvents = new[]
            {
                "message sent",
                "uow-message handled",
                "uow rolled back",
                "uow cleaned up",
            };

            Assert.That(events, Is.EqualTo(expectedEvents));
        }

        void RegisterEvent(string description)
        {
            _events.Enqueue(description);
        }
    }
}
