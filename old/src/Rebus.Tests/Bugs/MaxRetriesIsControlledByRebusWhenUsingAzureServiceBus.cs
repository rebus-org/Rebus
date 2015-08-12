using System;
using System.Diagnostics;
using System.Threading;
using Castle.MicroKernel.Facilities;
using NUnit.Framework;
using Rebus.AzureServiceBus;
using Rebus.Configuration;
using Rebus.Logging;
using Rebus.Tests.Contracts.Transports.Factories;
using Timer = System.Timers.Timer;

namespace Rebus.Tests.Bugs
{
    [TestFixture]
    public class MaxRetriesIsControlledByRebusWhenUsingAzureServiceBus : FixtureBase
    {
        [TestCase(3)]
        [TestCase(10)]
        [TestCase(20)]
        [TestCase(30)]
        [TestCase(50)]
        [TestCase(100)]
        public void WorksWithSomeNumberOfRetries(int numberOfRetries)
        {
            // arrange
            var messageMovedToErrorQueueEvent = new ManualResetEvent(false);
            var adapter = TrackDisposable(new BuiltinContainerAdapter());

            var deliveryCount = 0;
            adapter.Handle<string>(str =>
            {
                deliveryCount++;
                throw new FacilityException("wut?");
            });

            InitializeBus(numberOfRetries, adapter, messageMovedToErrorQueueEvent);

            var stopwatch = Stopwatch.StartNew();
            using (var infoTimer = new Timer(2000))
            {
                infoTimer.Elapsed += delegate
                {
                    Console.WriteLine("{0} delivery attempts - {1:0.0}s elapsed",
                        deliveryCount, stopwatch.Elapsed.TotalSeconds);
                };
                infoTimer.Start();

                // act
                adapter.Bus.SendLocal("ACT!!11");

                var timeout = (numberOfRetries*0.5).Seconds();
                messageMovedToErrorQueueEvent.WaitUntilSetOrDie(timeout, "Only managed to track {0} deliveries", deliveryCount);
                Thread.Sleep(1.Seconds());
            }

            // assert
            Assert.That(deliveryCount, Is.EqualTo(numberOfRetries));
        }

        void InitializeBus(int numberOfRetries, BuiltinContainerAdapter adapter, ManualResetEvent poisonEventToSet)
        {
            RebusLoggerFactory.Current = new ConsoleLoggerFactory(false) { MinLevel = LogLevel.Warn };

            var connectionString = AzureServiceBusMessageQueueFactory.ConnectionString;
            
            using (var queue = new AzureServiceBusMessageQueue(connectionString, "test_input"))
            {
                queue.Delete();
            }

            Configure.With(adapter)
                .Transport(t => t.UseAzureServiceBus(connectionString, "test_input", "error"))
                .Behavior(b => b.SetMaxRetriesFor<FacilityException>(numberOfRetries))
                .Events(e => e.PoisonMessage += (bus, message, info) => poisonEventToSet.Set())
                .CreateBus()
                .Start(1);
        }
    }
}