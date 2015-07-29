using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Transactions;
using NUnit.Framework;
using Rebus.AzureServiceBus;
using Rebus.Bus;
using Rebus.Extensions;
using Rebus.Logging;
using Rebus.Tests.Contracts.Transports.Factories;

namespace Rebus.Tests.Transports.Azure
{
    [TestFixture, Category(TestCategories.Azure)]
    public class TestAzureServiceBusMessageQueue_Queues : FixtureBase
    {
        const string InputQueueName = "test_competing_input";

        protected override void DoSetUp()
        {
            RebusLoggerFactory.Current = new ConsoleLoggerFactory(false) { MinLevel = LogLevel.Warn };
        }

        [TestCase(100, 3)]
        [TestCase(1000, 10, Ignore = true)]
        public void CanDoCompetingConsumers(int messageCount, int threadCount)
        {
            using (CreateQueue(InputQueueName).Purge()) { }

            var keepRunning = true;
            var lastMessageReceivedTime = DateTime.UtcNow;
            var receivedMessagesDistribution = new ConcurrentDictionary<int, int>();

            var receivers = Enumerable.Range(0, threadCount)
                .Select(i =>
                {
                    var queue = TrackDisposable(CreateQueue(InputQueueName));
                    var number = i + 1;

                    return new Thread(() =>
                    {
                        Console.WriteLine("Receiver {0} started", number);

                        while (keepRunning)
                        {
                            using (var tx = new TransactionScope())
                            {
                                var receivedMessage = queue.ReceiveMessage(new AmbientTransactionContext());

                                if (receivedMessage != null)
                                {
                                    receivedMessagesDistribution.AddOrUpdate(number, (key) => 1, (key, value) => value + 1);
                                    Console.Write(".");
                                    lastMessageReceivedTime = DateTime.UtcNow;
                                }

                                tx.Complete();
                            }
                        }

                        Console.WriteLine("Receiver {0} stopped", number);
                    });
                })
                .ToList();

            var sender = CreateQueue("test_competing_sender");

            Console.WriteLine("Sending {0} messages", messageCount);
            messageCount.Times(() => sender.Send(InputQueueName, new TransportMessageToSend
            {
                Headers = new Dictionary<string, object>(),
                Body = Encoding.UTF8.GetBytes("w00000t!")
            }, new NoTransaction()));

            Console.WriteLine("Starting {0} receivers", receivers.Count);
            receivers.ForEach(r => r.Start());

            lastMessageReceivedTime = DateTime.UtcNow;

            while (lastMessageReceivedTime.ElapsedUntilNow() < 3.Seconds())
            {
                Console.WriteLine("Waiting...");
                Thread.Sleep(2.Seconds());
            }

            Console.WriteLine("Stopping receivers...");
            keepRunning = false;
            receivers.ForEach(r => r.Join());

            Console.WriteLine("Got {0} messages distributed among workers like this:", receivedMessagesDistribution.Sum(d => d.Value));
            Console.WriteLine(string.Join(Environment.NewLine, receivedMessagesDistribution.Select(kvp => string.Format("{0:000}: {1}", kvp.Key, new string('=', kvp.Value)))));
        }

        static AzureServiceBusMessageQueue CreateQueue(string inputQueueName)
        {
            return new AzureServiceBusMessageQueue(AzureServiceBusMessageQueueFactory.ConnectionString, inputQueueName);
        }
    }
}