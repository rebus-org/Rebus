using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading;
using System.Transactions;
using NUnit.Framework;
using Rebus.Logging;
using Rebus.Tests.Transports.Rabbit;

namespace Rebus.Tests.Integration
{
    [TestFixture, Category(TestCategories.Rabbit)]
    public class TestRebusOnRabbit : RabbitMqFixtureBase
    {
        protected override void DoSetUp()
        {
            RebusLoggerFactory.Current = new NullLoggerFactory();
        }

        [TestCase(100, 5)]
        [TestCase(100, 10)]
        [TestCase(1000, 5)]
        [TestCase(1000, 10)]
        [TestCase(10000, 5, Ignore = true)]
        [TestCase(10000, 10, Ignore = true)]
        public void CanSendAndReceiveMessages(int messageCount, int numberOfWorkers)
        {
            const string senderQueueName = "test.rabbit.sender";
            const string receiverQueueName = "test.rabbit.receiver";

            var receivedMessages = new ConcurrentBag<string>();

            var resetEvent = new ManualResetEvent(false);
            
            var sender = CreateBus(senderQueueName, new HandlerActivatorForTesting()).Start();

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
            using (var tx = new TransactionScope())
            {
                var counter = 0;
                
                messageCount.Times(() => sender.Send(receiverQueueName, "message #" + (counter++).ToString()));

                tx.Complete();
            }
            var totalSeconds = stopwatch.Elapsed.TotalSeconds;
            Console.WriteLine("Sending {0} messages took {1:0.0} s - that's {2:0} msg/s",
                              messageCount, totalSeconds, messageCount/totalSeconds);

            stopwatch = Stopwatch.StartNew();
            receiver.Start(numberOfWorkers);

            if (!resetEvent.WaitOne(TimeSpan.FromSeconds(messageCount*0.01)))
            {
                Assert.Fail("Didn't receive all messages within timeout");
            }
            totalSeconds = stopwatch.Elapsed.TotalSeconds;

            Console.WriteLine("Receiving {0} messages took {1:0.0} s - that's {2:0} msg/s",
                              messageCount, totalSeconds, messageCount / totalSeconds);
        }
    }
}