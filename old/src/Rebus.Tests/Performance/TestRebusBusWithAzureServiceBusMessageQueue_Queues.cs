using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Transactions;
using NUnit.Framework;
using Rebus.AzureServiceBus;
using Rebus.Bus;
using Rebus.Configuration;
using Rebus.Tests.Contracts.Transports.Factories;
using Rebus.Tests.Integration;
using Shouldly;
using Rebus.Logging;
using Timer = System.Timers.Timer;

namespace Rebus.Tests.Performance
{
    [TestFixture, Category(TestCategories.Azure)]
    public class TestRebusBusWithAzureServiceBusMessageQueue_Queues : FixtureBase
    {
        const string QueueName1 = "perftest.bus1";
        const string QueueName2 = "perftest.bus2";

        BuiltinContainerAdapter adapter1;
        BuiltinContainerAdapter adapter2;

        protected override void DoSetUp()
        {
            RebusLoggerFactory.Current = new ConsoleLoggerFactory(false) { MinLevel = LogLevel.Warn };

            PurgeQueue(QueueName1);
            PurgeQueue(QueueName2);

            adapter1 = TrackDisposable(new BuiltinContainerAdapter());

            Configure.With(adapter1)
                     .Transport(t => t.UseAzureServiceBus(AzureServiceBusMessageQueueFactory.ConnectionString, QueueName1, "error"))
                     .CreateBus()
                     .Start();

            adapter2 = TrackDisposable(new BuiltinContainerAdapter());

            var receiverBus =
                (RebusBus)Configure
                               .With(adapter2)
                               .Transport(t => t.UseAzureServiceBus(AzureServiceBusMessageQueueFactory.ConnectionString, QueueName2, "error"))
                               .CreateBus();

            receiverBus.Start(10);

            RebusLoggerFactory.Current = new ConsoleLoggerFactory(false) {MinLevel = LogLevel.Warn};
        }

        [TestCase(10)]
        [TestCase(100)]
        [TestCase(1000, Ignore = TestCategories.IgnoreLongRunningTests)]
        [TestCase(5000, Ignore = TestCategories.IgnoreLongRunningTests)]
        [TestCase(10000, Ignore = TestCategories.IgnoreLongRunningTests)]
        [TestCase(20000, Ignore = TestCategories.IgnoreLongRunningTests)]
        public void CanSendAndReceiveManyMessagesReliably(int numberOfMessages)
        {
            using (var statusTimer = new Timer())
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

                statusTimer.Elapsed += (sender, args) => Console.WriteLine("Sent {0} messages, received {1} messages",
                                                                           sentMessageIds.Count, receivedMessages.Count);
                statusTimer.Interval = 2000;
                statusTimer.Start();

                adapter2.Handle<SomeNumberedMessage>(msg =>
                    {
                        receivedMessages.Add(msg);

                        if (receivedMessages.Count >= numberOfMessages)
                        {
                            Console.WriteLine("Setting the reset event!");
                            resetEvent.Set();
                        }
                    });

                var rebusRouting = adapter1.Bus.Advanced.Routing;
                var stopwatch = Stopwatch.StartNew();

                // send first half as batch
                var batchSize = messages.Count/2;

                var firstBatch = messages.Take(batchSize)
                                         .ToList();

                var theRest = messages.Skip(batchSize)
                                      .ToList();

                Console.WriteLine("Sending the first {0} messages in batches", firstBatch.Count);

                foreach (var msgBatch in firstBatch.Partition(100))
                {
                    Console.WriteLine("Sending batch of {0} messages", msgBatch.Count);

                    var complete = false;
                    var attempts = 0;

                    do
                    {
                        try
                        {
                            using (var scope = new TransactionScope())
                            {
                                foreach (var msg in msgBatch)
                                {
                                    rebusRouting.Send(QueueName2, msg);

                                    sentMessageIds.Add(msg.Id);
                                }

                                scope.Complete();
                            }

                            complete = true;
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine("An error occurred while sending batch of {0} msgs: {1}",
                                              msgBatch.Count, e);

                            attempts++;

                            if (attempts >= 5)
                            {
                                throw;
                            }

                            Thread.Sleep(1.Seconds());
                        }
                    } while (!complete);
                }

                Console.WriteLine("Sending the next {0} messages one at a time", theRest.Count);

                foreach (var msg in theRest)
                {
                    var attempts = 0;
                    var complete = false;

                    do
                    {
                        try
                        {
                            rebusRouting.Send(QueueName2, msg);
                            sentMessageIds.Add(msg.Id);
                            complete = true;
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine("An error occurred while attempting to send single message: {0}", e);

                            attempts++;

                            if (attempts >= 5)
                            {
                                throw;
                            }

                            Thread.Sleep(1.Seconds());
                        }
                    } while (!complete);
                }

                var totalSeconds = stopwatch.Elapsed.TotalSeconds;
                Console.WriteLine("Sending {0} messages took {1:0.0} s - that's {2:0} msg/s",
                                  numberOfMessages, totalSeconds, numberOfMessages/totalSeconds);

                var timeout = TimeSpan.FromSeconds(15 + numberOfMessages/10.0);
                Console.WriteLine("Waiting with {0} timeout", timeout);
                
                if (!resetEvent.WaitOne(timeout))
                {
                    Assert.Fail(@"Did not receive all {0} messages within {1} timeout

{2}",
                                numberOfMessages, timeout, FormatReport(sentMessageIds, receivedMessages));
                }

                Console.WriteLine("Waiting some extra time for possible duplicates to arrive");

                // wait and see if we have duplicates
                Thread.Sleep(2.Seconds());

                Console.WriteLine("Checking stuff");
                
                receivedMessages.Count.ShouldBe(numberOfMessages);

                Console.WriteLine("Checking {0} messages", sentMessageIds.Count);

                var formattedReport = FormatReport(sentMessageIds, receivedMessages);

                foreach (var id in sentMessageIds)
                {
                    var messagesWithThatId = receivedMessages.Count(m => m.Id == id);

                    Assert.That(messagesWithThatId, Is.EqualTo(1),
                                @"Expected exactly 1 message with ID {0}, but there was {1}!!

{2}",
                                id, messagesWithThatId, formattedReport);
                }

                Console.WriteLine("All done!");
            }

            Console.WriteLine("Disposed!!");
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

        void PurgeQueue(string queueName)
        {
            new AzureServiceBusMessageQueue(AzureServiceBusMessageQueueFactory.ConnectionString, queueName).Purge();
        }
    }
}