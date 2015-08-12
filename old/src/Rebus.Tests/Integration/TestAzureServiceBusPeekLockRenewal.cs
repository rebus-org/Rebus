using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Rebus.AzureServiceBus;
using Rebus.Configuration;
using Rebus.Logging;
using Rebus.Tests.Contracts.Transports.Factories;

namespace Rebus.Tests.Integration
{
    [TestFixture]
    public class TestAzureServiceBusPeekLockRenewal : FixtureBase
    {
        const string QueueName = "peekaboo";

        readonly string connectionString = AzureServiceBusMessageQueueFactory.ConnectionString;

        BuiltinContainerAdapter adapter;
        ConcurrentQueue<string> events;

        protected override void DoSetUp()
        {
            events = new ConcurrentQueue<string>();

            adapter = new BuiltinContainerAdapter();

            TrackDisposable(adapter);

            using (var queue = new AzureServiceBusMessageQueue(connectionString, QueueName))
            {
                queue.Delete();
            }

            AzureServiceBusMessageQueue.PeekLockDurationOnQueueInitialization = TimeSpan.FromMinutes(1);

            Configure.With(adapter)
                .Logging(l => l.Console(minLevel: LogLevel.Info))
                .Transport(t => t.UseAzureServiceBus(connectionString, QueueName, "error")
                    .AutomaticallyRenewPeekLockEvery(TimeSpan.FromMinutes(0.8)))
                .CreateBus()
                .Start();
        }

        [Test]
        public void PeekLockRenewalJustWorks()
        {
            var done = new ManualResetEvent(false);

            adapter.HandleAsync(async (string str) =>
            {
                LogEvent("entered!");
                
                await Task.Delay(TimeSpan.FromMinutes(1));

                LogEvent("waited one minute");

                await Task.Delay(TimeSpan.FromMinutes(1));

                LogEvent("waited two minutes");
                
                await Task.Delay(TimeSpan.FromMinutes(1));

                LogEvent("waited three minutes");

                LogEvent("done!");

                done.Set();
            });

            Console.WriteLine("Starting!");
            adapter.Bus.SendLocal("hej");

            done.WaitUntilSetOrDie(TimeSpan.FromMinutes(5));

            Console.WriteLine("Done!");
            Console.WriteLine();

            Console.WriteLine(@"LOG---------------------------------------------------------------------
{0}
------------------------------------------------------------------------", string.Join(Environment.NewLine, events));
        }

        void LogEvent(string text)
        {
            Console.WriteLine(text);
            events.Enqueue(text);
        }
    }
}