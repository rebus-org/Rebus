using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading;
using System.Transactions;
using NUnit.Framework;
using RabbitMQ.Client;
using Rebus.Logging;
using Rebus.RabbitMQ;
using Rebus.Tests.Transports.Rabbit;

namespace Rebus.Tests.Integration
{
    [TestFixture, Category(TestCategories.Rabbit)]
    public class TestRebusOnRabbit : RabbitMqFixtureBase
    {
        protected override void DoSetUp()
        {
            RebusLoggerFactory.Current = new ConsoleLoggerFactory(false) {MinLevel = LogLevel.Warn};
        }

        [Test]
        public void DoesntActuallyConnectWhenCreatingTheFactory()
        {
            using(var connectionManager = new ConnectionManager("amqp://would_throw_if_it_connected_immediately,amqp://would_also_throw_if_it_connected_immediately", "w00t!"))
            {
                // arrange

                // act

                //var connection = connectionManager.GetConnection();

                // assert
            }
        }

        [Test]
        public void WillCreateInputAndErrorQueue()
        {
            var testRabbitQueues = "test.rabbit.queues";
            CreateBus(testRabbitQueues, new HandlerActivatorForTesting()).Start(1);


            using (var connection = new ConnectionFactory {Uri = ConnectionString}.CreateConnection())
            using (var model = connection.CreateModel())
            {
                Assert.DoesNotThrow(() => model.BasicGet(testRabbitQueues, true));
                Assert.DoesNotThrow(() => model.BasicGet(testRabbitQueues + ".error", true));
            }
        }

        [TestCase(1, 1)]
        [TestCase(100, 3)]
        [TestCase(10000, 3)]
        [TestCase(10000, 5, Ignore = TestCategories.IgnoreLongRunningTests)]
        [TestCase(100000, 3, Ignore = TestCategories.IgnoreLongRunningTests)]
        [TestCase(100000, 5, Ignore = TestCategories.IgnoreLongRunningTests)]
        public void CanSendAndReceiveMessages(int messageCount, int numberOfWorkers)
        {
            const string senderQueueName = "test.rabbit.sender";
            const string receiverQueueName = "test.rabbit.receiver";

            var receivedMessages = new ConcurrentBag<string>();

            var resetEvent = new ManualResetEvent(false);
            
            var sender = CreateBus(senderQueueName, new HandlerActivatorForTesting()).Start(1);

            var receiver = CreateBus(receiverQueueName,
                                     new HandlerActivatorForTesting()
                                         .Handle<string>(str =>
                                                             {
                                                                 receivedMessages.Add(str);
                                                                 
                                                                 if (receivedMessages.Count == messageCount)
                                                                 {
                                                                     resetEvent.Set();
                                                                 }
                                                             }));

            var stopwatch = Stopwatch.StartNew();
            using (var tx = new TransactionScope(TransactionScopeOption.Required, TimeSpan.FromMinutes(5)))
            {
                var counter = 0;

                messageCount.Times(() => sender.Routing.Send(receiverQueueName, "message #" + (counter++).ToString()));

                tx.Complete();
            }
            var totalSeconds = stopwatch.Elapsed.TotalSeconds;
            Console.WriteLine("Sending {0} messages took {1:0.0} s - that's {2:0} msg/s",
                              messageCount, totalSeconds, messageCount/totalSeconds);

            stopwatch = Stopwatch.StartNew();
            receiver.Start(numberOfWorkers);

            var accountForLatency = TimeSpan.FromSeconds(10);
            if (!resetEvent.WaitOne(TimeSpan.FromSeconds(messageCount*0.02) + accountForLatency))
            {
                Assert.Fail("Didn't receive all messages within timeout - only {0} messages were received", receivedMessages.Count);
            }
            totalSeconds = stopwatch.Elapsed.TotalSeconds;

            Console.WriteLine("Receiving {0} messages took {1:0.0} s - that's {2:0} msg/s",
                              messageCount, totalSeconds, messageCount / totalSeconds);
        }
    }
}