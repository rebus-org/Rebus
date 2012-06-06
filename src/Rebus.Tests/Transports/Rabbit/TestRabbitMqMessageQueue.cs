using System;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using NUnit.Framework;
using Rebus.Logging;
using Rebus.RabbitMQ;
using Shouldly;

namespace Rebus.Tests.Transports.Rabbit
{
    [TestFixture, Category(TestCategories.Rabbit)]
    public class TestRabbitMqMessageQueue : RabbitMqFixtureBase
    {
        protected override void DoSetUp()
        {
            RebusLoggerFactory.Current = new NullLoggerFactory();
        }

        /// <summary>
        /// First:
        ///     Sending 1000 messages took 0,1 s - that's 7988 msg/s
        ///     Receiving 1000 messages spread across 10 consumers took 6,1 s - that's 165 msg/s
        /// 
        ///     Sending 100000 messages took 11,5 s - that's 8676 msg/s
        ///     Receiving 100000 messages spread across 10 consumers took 6,4 s - that's 15665 msg/s
        /// 
        ///     Conclusion: Seems there's a pretty large overhead in establishing a subscription...
        /// </summary>
        [TestCase(100, 10)]
        [TestCase(1000, 10)]
        [TestCase(10000, 10)]
        public void CanSendAndReceiveMessages(int count, int consumers)
        {
            const string senderInputQueue = "test.rabbit.sender";
            const string receiverInputQueue = "test.rabbit.receiver";

            var sender = GetQueue(senderInputQueue);
            var receiver = GetQueue(receiverInputQueue);

            var totalMessageCount = count*consumers;

            var stopwatch = Stopwatch.StartNew();

            Console.WriteLine("Sending {0} messages", totalMessageCount);
            Enumerable.Range(0, totalMessageCount).ToList()
                .ForEach(i => sender.Send(receiverInputQueue, new TransportMessageToSend { Body = Encoding.UTF7.GetBytes("w00t! message " + i) }));

            var totalSeconds = stopwatch.Elapsed.TotalSeconds;

            Console.WriteLine("Sending {0} messages took {1:0.0} s - that's {2:0} msg/s", totalMessageCount, totalSeconds, totalMessageCount / totalSeconds);

            Thread.Sleep(1.Seconds());

            Console.WriteLine("Receiving {0} messages", totalMessageCount);
            long receivedMessageCount = 0;

            stopwatch = Stopwatch.StartNew();

            var threads = Enumerable.Range(0, consumers)
                .Select(i => new Thread(_ =>
                    {
                        var gotNoMessageCount = 0;
                        do
                        {
                            var receivedTransportMessage = receiver.ReceiveMessage();
                            if (receivedTransportMessage == null)
                            {
                                gotNoMessageCount++;
                                continue;
                            }
                            Encoding.UTF7.GetString(receivedTransportMessage.Body).ShouldStartWith("w00t! message ");
                            Interlocked.Increment(ref receivedMessageCount);
                        } while (gotNoMessageCount < 3);
                    }))
                .ToList();

            threads.ForEach(t => t.Start());
            threads.ForEach(t => t.Join());

            totalSeconds = stopwatch.Elapsed.TotalSeconds;
            
            Console.WriteLine("Receiving {0} messages spread across {1} consumers took {2:0.0} s - that's {3:0} msg/s",
                              totalMessageCount, consumers, totalSeconds, totalMessageCount/totalSeconds);

            receivedMessageCount.ShouldBe(totalMessageCount);
        }

        RabbitMqMessageQueue GetQueue(string queueName)
        {
            var queue = new RabbitMqMessageQueue(ConnectionString, queueName, queueName + ".error");
            toDispose.Add(queue);
            return queue.PurgeInputQueue();
        }
    }
}