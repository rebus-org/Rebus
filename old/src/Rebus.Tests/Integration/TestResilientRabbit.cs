using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using NUnit.Framework;
using Rebus.Configuration;
using Rebus.RabbitMQ;
using Rebus.Tests.Transports.Rabbit;
using Shouldly;

namespace Rebus.Tests.Integration
{
    [TestFixture, Description("This test creates a publisher and two subscribers and attempts to publish a stream of messages while the Rabbit MQ server process gets restarted.")]
    [Category(TestCategories.Rabbit)]
    [Category(TestCategories.Integration)]
    [Ignore]
    public class TestResilientRabbit : FixtureBase
    {
        readonly List<int> sub1Received = new List<int>();
        readonly List<int> sub2Received = new List<int>();

        IBus publisher;
        IBus sub1;
        IBus sub2;

        protected override void DoSetUp()
        {
            publisher = Create("test.resilience.publisher");
            sub1 = Create("test.resilience.subscriber1", e => sub1Received.Add(e.Number));
            sub2 = Create("test.resilience.subscriber2", e => sub2Received.Add(e.Number));
        }

        protected override void DoTearDown()
        {
            CleanUpTrackedDisposables();

            Rabbit("start");
        }

        [TestCase(100)]
        public void NoMessagesAreLostEventThoughTheRabbitServiceRestartsInTheMiddle(int eventCount)
        {
            // arrange
            sub1.Subscribe<SomeEvent>();
            sub2.Subscribe<SomeEvent>();

            Thread.Sleep(1.Seconds());

            var restartRabbitSignal = new ManualResetEvent(false);

            // act
            var publisherWork = new Thread(() =>
                {
                    var eventsToPublish = Enumerable.Range(0, eventCount)
                        .Select(i => new SomeEvent { Number = i })
                        .ToList();

                    var counter = 0;

                    foreach (var eventToPublish in eventsToPublish)
                    {
                        counter++;
                        publisher.Publish(eventToPublish);

                        if (counter == eventCount - eventCount / 5)
                        {
                            restartRabbitSignal.Set();
                        }

                        if (counter % counter / 10 == 0)
                        {
                            Thread.Sleep(100.Milliseconds());
                        }
                    }
                });
            publisherWork.Start();

            restartRabbitSignal.WaitOne();

            Console.WriteLine("Stopping RabbitMQ...");
            Rabbit("stop");

            Thread.Sleep(2.Seconds());

            Console.WriteLine("Starting RabbitMQ...");
            Rabbit("start");

            publisherWork.Join();

            Thread.Sleep(1.Seconds() + (eventCount * 0.1).Seconds());

            // assert
            var sub1Deduped = sub1Received.Distinct().OrderBy(i => i).ToList();
            var sub2Deduped = sub2Received.Distinct().OrderBy(i => i).ToList();

            sub1Deduped.Count.ShouldBe(eventCount);
            sub2Deduped.Count.ShouldBe(eventCount);

            sub1Deduped.ShouldBe(Enumerable.Range(0, eventCount).ToList());
            sub2Deduped.ShouldBe(Enumerable.Range(0, eventCount).ToList());
        }

        static void Rabbit(string what)
        {
            var systemRoot = Environment.GetEnvironmentVariable("SYSTEMROOT");

            if (string.IsNullOrEmpty(systemRoot))
            {
                Console.WriteLine("WARNING: %SYSTEMROOT% was null - defaulting to C:\\Windows");
                systemRoot = @"C:\Windows";
            }

            var command = Path.Combine(systemRoot, "system32", "net.exe");
            var args = string.Format("{0} RabbitMQ", what);

            try
            {
                Process.Start(command, args);
            }
            catch (Exception e)
            {
                Console.WriteLine("ERROR executing {0} {1}: {2}", command, args, e);
            }
        }

        class SomeEvent
        {
            public int Number { get; set; }
        }

        IBus Create(string inputQueueName, Action<SomeEvent> eventHandler = null)
        {
            RabbitMqFixtureBase.DeleteQueue(inputQueueName);

            var adapter = new BuiltinContainerAdapter();
            if (eventHandler != null) adapter.Handle(eventHandler);

            Configure.With(adapter)
                .Transport(t => t.UseRabbitMq(RabbitMqFixtureBase.ConnectionString,
                                              inputQueueName,
                                              "test.resilience.error")
                                    .ManageSubscriptions())
                .CreateBus()
                .Start();

            TrackDisposable(adapter);

            return adapter.Bus;
        }
    }
}