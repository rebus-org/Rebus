using System;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Rebus.Transports.Rabbit;
using Shouldly;

namespace Rebus.Tests.Transports.Rabbit
{
    [TestFixture, Category(TestCategories.Rabbit)]
    public class TestRabbitMqMessageQueue : RabbitMqFixtureBase
    {
        [TestCase(100, 10)]
        [TestCase(1000, 10, Ignore = TestCategories.IgnoreLongRunningTests)]
        [TestCase(10000, 10, Ignore = TestCategories.IgnoreLongRunningTests)]
        public void CanSendAndReceiveMessages(int count, int consumers)
        {
            var sender = new RabbitMqMessageQueue(ConnectionString, "test.rabbit.sender").PurgeInputQueue();
            
            const string consumerInputQueue = "test.rabbit.receiver";

            var competingConsumers = Enumerable.Range(0, consumers)
                .Select(i => new RabbitMqMessageQueue(ConnectionString, consumerInputQueue).PurgeInputQueue())
                .ToArray();

            var messageCount = count*consumers;

            Console.WriteLine("Sending {0} messages", messageCount);
            messageCount.Times(() => sender.Send(consumerInputQueue, new TransportMessageToSend { Body = Encoding.UTF7.GetBytes("w00t!") }));

            Console.WriteLine("Receiving {0} messages", messageCount);
            long receivedMessageCount = 0;

            var stopwatch = Stopwatch.StartNew();

            Parallel.For(0, consumers,
                         i => count.Times(() =>
                                              {
                                                  var receivedTransportMessage = competingConsumers[i].ReceiveMessage();
                                                  if (receivedTransportMessage == null) return;
                                                  Encoding.UTF7.GetString(receivedTransportMessage.Body).ShouldBe("w00t!");
                                                  Interlocked.Increment(ref receivedMessageCount);
                                              }));

            var totalSeconds = stopwatch.Elapsed.TotalSeconds;
            
            Console.WriteLine("Receiving {0} messages spread across {1} consumers took {2:0.0} s - that's {3:0} msg/s",
                              messageCount, consumers, totalSeconds, messageCount/totalSeconds);
        }
    }
}