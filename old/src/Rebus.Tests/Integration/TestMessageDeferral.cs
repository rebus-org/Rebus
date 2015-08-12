using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using NUnit.Framework;
using Rebus.Logging;
using Rebus.Persistence.InMemory;
using Rebus.Shared;
using Rebus.Tests.Integration.Factories;
using Shouldly;
using System.Linq;
using Timer = System.Timers.Timer;

namespace Rebus.Tests.Integration
{
    [TestFixture(typeof(InternalTimeoutManagerFactory), typeof(MsmqBusFactory)), Category(TestCategories.Integration)]
    [TestFixture(typeof(ExternalTimeoutManagerFactory), typeof(MsmqBusFactory)), Category(TestCategories.Integration)]
    [TestFixture(typeof(InternalTimeoutManagerFactory), typeof(RabbitBusFactory)), Category(TestCategories.Integration)]
    public class TestMessageDeferral<TTimeoutManagerFactory, TBusFactory> : RebusBusMsmqIntegrationTestBase
        where TTimeoutManagerFactory : ITimeoutManagerFactory, new()
        where TBusFactory : IBusFactory, new()
    {
        IBus bus;
        HandlerActivatorForTesting handlerActivator;
        TTimeoutManagerFactory timeoutManagerFactory;

        protected override void DoSetUp()
        {
            timeoutManagerFactory = new TTimeoutManagerFactory();
            timeoutManagerFactory.Initialize(new TBusFactory());

            handlerActivator = new HandlerActivatorForTesting();
            bus = timeoutManagerFactory.CreateBus("test.deferral", handlerActivator);

            var logFileName = string.Format("log-{0}-{1}.txt", typeof(TTimeoutManagerFactory).Name, typeof(TBusFactory).Name);
            var logFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, logFileName);

            if (File.Exists(logFilePath))
            {
                File.Delete(logFilePath);
            }

            RebusLoggerFactory.Current = new GhettoFileLoggerFactory(logFilePath)
                .WithFilter(m => m.LoggerType == typeof(InMemoryTimeoutStorage));

            timeoutManagerFactory.StartAll();
        }

        protected override void DoTearDown()
        {
            timeoutManagerFactory.CleanUp();
        }

        [Test]
        public void CanPreserveCustomHeadersOnDeferredMessage()
        {
            // arrange
            var deferredMessageReceived = new ManualResetEvent(false);
            IDictionary<string, object> receivedHeaders = null;
            handlerActivator.Handle<MessageWithText>(m =>
            {
                receivedHeaders = MessageContext.GetCurrent().Headers;

                deferredMessageReceived.Set();
            });

            var message = new MessageWithText { Text = "hello" };
            bus.AttachHeader(message, Headers.UserName, "joe");
            bus.AttachHeader(message, "test-custom1", "bimmelim!");
            bus.AttachHeader(message, "test-custom2", "w00t!");

            // act
            bus.Defer(1.Seconds(), message);
            deferredMessageReceived.WaitUntilSetOrDie(3.Seconds());

            // assert
            receivedHeaders.ShouldNotBe(null);
            receivedHeaders.ShouldContainKeyAndValue(Headers.UserName, "joe");
            receivedHeaders.ShouldContainKeyAndValue("test-custom1", "bimmelim!");
            receivedHeaders.ShouldContainKeyAndValue("test-custom2", "w00t!");
        }


        [Test]
        public void CanMakeSimpleDeferralOfMessages()
        {
            // arrange
            var messages = new List<Tuple<string, DateTime>>();
            var deferredMessageReceived = new AutoResetEvent(false);
            handlerActivator.Handle<MessageWithText>(m =>
            {
                Console.WriteLine("Got message with '{0}'", m.Text);
                messages.Add(new Tuple<string, DateTime>(m.Text, DateTime.UtcNow));
                deferredMessageReceived.Set();
            });

            var timeOfDeferral = DateTime.UtcNow;
            var acceptedTolerance = 2.Seconds();

            // act
            bus.Defer(10.Seconds(), new MessageWithText { Text = "deferred 10 seconds" });
            bus.Defer(5.Seconds(), new MessageWithText { Text = "deferred 5 seconds" });

            deferredMessageReceived.WaitUntilSetOrDie(TimeSpan.FromSeconds(10));
            deferredMessageReceived.WaitUntilSetOrDie(TimeSpan.FromSeconds(10));

            // assert
            messages.Count.ShouldBe(2);
            messages[0].Item1.ShouldBe("deferred 5 seconds");
            messages[0].Item2.ElapsedSince(timeOfDeferral).ShouldBeGreaterThan(5.Seconds() - acceptedTolerance);
            messages[0].Item2.ElapsedSince(timeOfDeferral).ShouldBeLessThan(5.Seconds() + acceptedTolerance);

            messages[1].Item1.ShouldBe("deferred 10 seconds");
            messages[1].Item2.ElapsedSince(timeOfDeferral).ShouldBeGreaterThan(10.Seconds() - acceptedTolerance);
            messages[1].Item2.ElapsedSince(timeOfDeferral).ShouldBeLessThan(10.Seconds() + acceptedTolerance);
        }

        [TestCase(200, 50)]
        [TestCase(500, 120)]
        [TestCase(1500, 200)]
        public void WorksReliablyWithManyTimeouts(int messageCount, int acceptedToleranceSec)
        {
            // arrange
            var receivedMessages = new ConcurrentList<Tuple<DateTime, DateTime, int>>();
            var sentMessages = new ConcurrentList<MessageWithExpectedReturnTime>();

            var resetEvent = new ManualResetEvent(false);
            handlerActivator
                .Handle<MessageWithExpectedReturnTime>(
                    m =>
                    {
                        receivedMessages.Add(Tuple.Create(m.ExpectedReturnTime, DateTime.UtcNow, m.MessageId));

                        var currentCount = receivedMessages.Count;

                        if (currentCount >= messageCount)
                        {
                            Console.WriteLine("Got {0} messages, setting reset event", currentCount);
                            resetEvent.Set();
                        }
                    });

            var acceptedTolerance = acceptedToleranceSec.Seconds();
            var random = new Random();
            var number = 0;

            // act
            messageCount.Times(() =>
                {
                    var delay = (random.Next(20) + 10).Seconds();
                    var message = new MessageWithExpectedReturnTime
                                      {
                                          ExpectedReturnTime = DateTime.UtcNow + delay,
                                          MessageId = number++
                                      };
                    bus.Defer(delay, message);
                    sentMessages.Add(message);
                });

            using (var timer = new Timer())
            {
                timer.Elapsed += (o, ea) => Console.WriteLine("{0}: got {1} messages", DateTime.UtcNow, receivedMessages.Count);
                timer.Interval = 3000;
                timer.Start();

                if (!resetEvent.WaitOne(150.Seconds()))
                {
                    Assert.Fail(@"Only {0} messages were received within the 45 s timeout!!

Here they are:
{1}",
                                receivedMessages.Count, GetMessagesAsText(receivedMessages, sentMessages));
                }

                // just wait a while, to be sure that more messages are not arriving
                Thread.Sleep(1000);
            }

            // assert
            if (receivedMessages.Count != sentMessages.Count)
            {
                Assert.Fail(@"Expected {0} messages to have been received, but we got {1}!!

Here they are:
{1}", sentMessages.Count, receivedMessages.Count, GetMessagesAsText(receivedMessages, sentMessages));
            }

            foreach (var messageTimes in receivedMessages)
            {
                var lowerBound = messageTimes.Item1 - acceptedTolerance;
                var upperBound = messageTimes.Item1 + acceptedTolerance;

                if (messageTimes.Item2 <= lowerBound || messageTimes.Item2 >= upperBound)
                {
                    Assert.Fail("Something is wrong with message # {0} - the time of receiving it ({1}) was outside accepted bounds {2} - {3} - the timeout was set to {4}",
                        messageTimes.Item3, messageTimes.Item2, lowerBound, upperBound, messageTimes.Item1);
                }
            }
        }

        static string GetMessagesAsText(ConcurrentList<Tuple<DateTime, DateTime, int>> receivedMessages, ConcurrentList<MessageWithExpectedReturnTime> sentMessages)
        {
            var receivedMessagesAsText =
                string.Join(Environment.NewLine,
                            receivedMessages
                                .Select(m => string.Join(";",
                                                         new[]
                                                             {
                                                                 m.Item3.ToString(),
                                                                 m.Item1.ToString(),
                                                                 m.Item2.ToString(),
                                                             })));

            var receivedMessageIds = receivedMessages
                .Select(m => m.Item3)
                .ToList();

            var sentButNotReceivedMessages = sentMessages
                .Where(m => !receivedMessageIds.Contains(m.MessageId))
                .ToList();

            return string.Format(@"Received messages:
{0}

Sent, but not received messages:
{1}", receivedMessagesAsText,
                                 string.Join(Environment.NewLine, sentButNotReceivedMessages.Select(m => m.MessageId)));
        }
    }

    public class MessageWithExpectedReturnTime
    {
        public int MessageId { get; set; }
        public DateTime ExpectedReturnTime { get; set; }
    }

    public class MessageWithText
    {
        public string Text { get; set; }
    }

    public class ConcurrentList<T> : IEnumerable<T>
    {
        readonly List<T> innerList = new List<T>();
        readonly object listAccessLock = new object();

        public IEnumerator<T> GetEnumerator()
        {
            lock (listAccessLock)
            {
                return new List<T>(innerList).GetEnumerator();
            }
        }

        public void Add(T item)
        {
            lock (listAccessLock)
            {
                innerList.Add(item);
            }
        }

        public int Count
        {
            get
            {
                lock (listAccessLock)
                {
                    return innerList.Count;
                }
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public T this[int index]
        {
            get
            {
                lock (listAccessLock)
                {
                    return innerList[index];
                }
            }
        }
    }
}
