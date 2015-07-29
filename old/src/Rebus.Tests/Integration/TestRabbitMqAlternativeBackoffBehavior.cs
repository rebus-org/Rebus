using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using NUnit.Framework;
using Rebus.Configuration;
using Rebus.Extensions;
using Rebus.Logging;
using Rebus.RabbitMQ;
using Rebus.Tests.Transports.Rabbit;

namespace Rebus.Tests.Integration
{
    [TestFixture, Category(TestCategories.Rabbit)]
    public class TestRabbitMqAlternativeBackoffBehavior : RabbitMqFixtureBase
    {
        const string InputQueueName = "test.backoff.input";
        BuiltinContainerAdapter handlerAdapter;

        protected override void DoSetUp()
        {
            using (var queue = new RabbitMqMessageQueue(ConnectionString, InputQueueName))
            {
                queue.PurgeInputQueue();
            }
        }

        protected override void DoTearDown()
        {
            CleanUpTrackedDisposables();

            DeleteQueue(InputQueueName);
        }

        [TestCase(false, 5)]
        [TestCase(true, 5)]
        public void RunTest(bool useLowLatencyBackoffStrategy, int iterations)
        {
            var adapter = new BuiltinContainerAdapter();
            var messageHasBeenReceived = new ManualResetEvent(false);
            adapter.Handle<string>(s => messageHasBeenReceived.Set());

            ConfigureBus(adapter, useLowLatencyBackoffStrategy);

            var bus = adapter.Bus;

            var recordedLatencies = new List<TimeSpan>();

            iterations.Times(() =>
            {
                // let things calm down
                Console.Write("Calming down.... ");
                Thread.Sleep(30.Seconds());

                Console.Write("Sending! ");
                var sendTime = DateTime.UtcNow;
                bus.SendLocal("w0000tamafooook!!1");

                Console.Write("waiting... ");
                messageHasBeenReceived.WaitUntilSetOrDie(20.Seconds());

                var elapsedUntilNow = sendTime.ElapsedUntilNow();
                Console.WriteLine("got the message - recorded latency of {0}", elapsedUntilNow);
                recordedLatencies.Add(elapsedUntilNow);
                messageHasBeenReceived.Reset();
            });

            Console.WriteLine("AVERAGE RECORDED LATENCY: {0:0.00} s", recordedLatencies.Average(t => t.TotalSeconds));
        }

        void ConfigureBus(BuiltinContainerAdapter adapter, bool useLowLatencyBackoffStrategy)
        {
            handlerAdapter = TrackDisposable(adapter);

            Configure.With(handlerAdapter)
                .Logging(l => l.ColoredConsole(LogLevel.Warn))
                .Transport(t => t.UseRabbitMq(ConnectionString, InputQueueName, "error")
                    .ManageSubscriptions())
                .Behavior(b =>
                {
                    if (useLowLatencyBackoffStrategy)
                    {
                        b.SetLowLatencyBackoffBehavior();
                    }
                })
                .CreateBus()
                .Start(1);
        }
    }
}