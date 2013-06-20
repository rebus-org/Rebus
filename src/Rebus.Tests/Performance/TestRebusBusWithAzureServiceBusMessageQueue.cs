using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using NUnit.Framework;
using Rebus.Configuration;
using Rebus.AzureServiceBus;
using Rebus.Tests.Configuration;
using Rebus.Tests.Contracts.Transports.Factories;
using Rebus.Tests.Integration;
using Shouldly;
using Rebus.Logging;

namespace Rebus.Tests.Performance
{
    [TestFixture, Category(TestCategories.Azure)]
    public class TestRebusBusWithAzureServiceBusMessageQueue : FixtureBase
    {
        const string QueueName1 = "perftest.bus1";
        const string QueueName2 = "perftest.bus2";
        
        BuiltinContainerAdapter adapter1;
        BuiltinContainerAdapter adapter2;

        protected override void DoSetUp()
        {
            PurgeQueue(QueueName1);
            PurgeQueue(QueueName2);

            adapter1 = new BuiltinContainerAdapter();

            Configure.With(adapter1)
                     .Logging(l => l.Console(minLevel: LogLevel.Warn))
                     .Transport(t => t.UseAzureServiceBus(AzureServiceBusMessageQueueFactory.ConnectionString, QueueName1, "error"))
                     .CreateBus()
                     .Start();

            adapter2 = new BuiltinContainerAdapter();

            Configure.With(adapter2)
                     .Logging(l => l.Console(minLevel: LogLevel.Warn))
                     .Transport(t => t.UseAzureServiceBus(AzureServiceBusMessageQueueFactory.ConnectionString, QueueName2, "error"))
                     .CreateBus()
                     .Start();
        }

        [TestCase(10)]
        [TestCase(100)]
        [TestCase(1000, Ignore = TestCategories.IgnoreLongRunningTests)]
        [TestCase(10000, Ignore = TestCategories.IgnoreLongRunningTests)]
        public void CanSendAndReceiveManyMessagesReliably(int numberOfMessages)
        {
            var messages = Enumerable.Range(1, numberOfMessages)
                                     .Select(n => new SomeNumberedMessage
                                                      {
                                                          Id = Guid.NewGuid(),
                                                          Text = string.Format("Message # {0}", n)
                                                      })
                                     .ToList();

            var receivedMessages = new ConcurrentList<SomeNumberedMessage>();
            var resetEvent = new ManualResetEvent(false);
            var sentMessageIds = new ConcurrentList<Guid>();

            adapter2.Handle<SomeNumberedMessage>(msg =>
                {
                    receivedMessages.Add(msg);

                    if (receivedMessages.Count%1000 == 0)
                    {
                        Console.WriteLine("Received {0} messages", receivedMessages.Count);
                    }

                    if (receivedMessages.Count >= numberOfMessages)
                    {
                        resetEvent.Set();
                    }
                });

            var rebusRouting = adapter1.Bus.Advanced.Routing;
            var stopwatch = Stopwatch.StartNew();

            foreach (var msg in messages)
            {
                rebusRouting.Send(QueueName2, msg);

                sentMessageIds.Add(msg.Id);

                if (sentMessageIds.Count%1000 == 0)
                {
                    Console.WriteLine("Sent {0} messages", sentMessageIds.Count);
                }
            }

            var totalSeconds = stopwatch.Elapsed.TotalSeconds;
            Console.WriteLine("Sending {0} messages took {1:0.0} s - that's {2:0} msg/s",
                              numberOfMessages, totalSeconds, numberOfMessages/totalSeconds);

            var timeout = TimeSpan.FromSeconds(15 + numberOfMessages/40.0);
            Console.WriteLine("Waiting with {0} timeout", timeout);
            if (!resetEvent.WaitOne(timeout))
            {
                Assert.Fail(@"Did not receive all {0} messages within {1} timeout

{2}",
                            numberOfMessages, timeout, FormatReport(sentMessageIds, receivedMessages));
            }

            // wait and see if we have duplicates
            Thread.Sleep(1000);

            receivedMessages.Count.ShouldBe(numberOfMessages);

            foreach (var id in sentMessageIds)
            {
                var messagesWithThatId = receivedMessages.Count(m => m.Id == id);

                Assert.That(messagesWithThatId, Is.EqualTo(1),
                            @"Expected exactly 1 message with ID {0}, but there was {1}!!

{2}",
                            id, messagesWithThatId, FormatReport(sentMessageIds, receivedMessages));
            }
        }

        string FormatReport(ConcurrentList<Guid> sentMessageIds, ConcurrentList<SomeNumberedMessage> receivedMessages)
        {
            var sentButNotReceivedIds = sentMessageIds.Where(i => !receivedMessages.Any(m => m.Id == i)).ToList();
            var duplicatedMessages = receivedMessages.GroupBy(m => m.Id)
                                                     .Where(g => g.Count() > 1)
                                                     .ToList();

            return string.Format(@"The following messages were sent but not received: {0}
(that was {1})

The following received messages were duplicated: {2}
(that was {3})",
                                 string.Join(", ", sentButNotReceivedIds),
                                 sentButNotReceivedIds.Count,
                                 string.Join(", ", duplicatedMessages.Select(m => m.Key)),
                                 duplicatedMessages.Count);
        }

        class SomeNumberedMessage
        {
            public string Text { get; set; }
            public Guid Id { get; set; }
        }

        protected override void DoTearDown()
        {
            adapter1.Dispose();
            adapter2.Dispose();
        }

        void PurgeQueue(string queueName)
        {
            new AzureServiceBusMessageQueue(AzureServiceBusMessageQueueFactory.ConnectionString, queueName).Purge();
        }
    }
}