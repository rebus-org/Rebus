using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using Rebus.Activation;
using Rebus.Bus;
using Rebus.Config;
using Rebus.Logging;
using Rebus.Persistence.SqlServer;
using Rebus.Tests.Extensions;
using Rebus.Transport.SqlServer;

namespace Rebus.Tests.Integration
{
    [TestFixture]
    public class TestBugWhenSendingMessagesInParallel : FixtureBase
    {
        readonly string _subscriptionsTableName = "subscriptions" + TestConfig.Suffix;
        readonly string _messagesTableName = "messages" + TestConfig.Suffix;

        IBus _bus1;
        IBus _bus2;
        IBus _bus3;

        ConcurrentQueue<string> _receivedMessages;

        protected override void SetUp()
        {
            _receivedMessages = new ConcurrentQueue<string>();

            _bus1 = CreateBus(TestConfig.QueueName("bus1"), async str => { });
            _bus2 = CreateBus(TestConfig.QueueName("bus2"), async str =>
            {
                _receivedMessages.Enqueue("bus2 got " + str);
            });
            _bus3 = CreateBus(TestConfig.QueueName("bus3"), async str =>
            {
                _receivedMessages.Enqueue("bus3 got " + str);
            });
        }

        IBus CreateBus(string inputQueueName, Func<string, Task> stringHandler)
        {
            var activator = new BuiltinHandlerActivator();

            activator.Handle(stringHandler);

            var bus = Configure.With(activator)
                .Logging(l => l.ColoredConsole(minLevel: LogLevel.Warn))
                .Transport(t => t.UseSqlServer(SqlTestHelper.ConnectionString, _messagesTableName, inputQueueName))
                .Subscriptions(s => s.StoreInSqlServer(SqlTestHelper.ConnectionString, _subscriptionsTableName, isCentralized: true))
                .Start();

            return Using(bus);
        }

        [Test]
        public async Task CheckRealisticScenarioWithSqlAllTheWay()
        {
            await Task.WhenAll(
                _bus2.Subscribe(typeof(string).FullName),
                _bus3.Subscribe(typeof(string).FullName)
                );

            await _bus1.Publish(typeof (string).FullName, "hej");

            await _receivedMessages.WaitUntil(q => q.Count >= 2);

            await Task.Delay(200);

            var receivedStrings = _receivedMessages.OrderBy(s => s).ToArray();

            Assert.That(receivedStrings, Is.EqualTo(new[]
            {
                "bus2 got hej",
                "bus3 got hej"
            }));
        }
    }
}