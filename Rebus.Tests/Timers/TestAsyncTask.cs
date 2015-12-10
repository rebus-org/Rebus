using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading.Tasks;
using NUnit.Framework;
using Rebus.Logging;
using Rebus.Threading;
#pragma warning disable 1998

namespace Rebus.Tests.Timers
{
    [TestFixture]
    public class TestAsyncTask : FixtureBase
    {
        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        [TestCase(5)]
        public async Task CanActuallyStopTaskWithLongInterval(int secondsToLetTheTaskRun)
        {
            using (var task = new TplAsyncTask("simulate-azure-service-bus-peek-lock-renewer", async () => { Console.WriteLine("INVOKED!!!"); }, new ConsoleLoggerFactory(false))
            {
                Interval = TimeSpan.FromMinutes(4.5)
            })
            {
                task.Start();

                Console.WriteLine($"Letting the task run for {secondsToLetTheTaskRun} seconds...");

                await Task.Delay(TimeSpan.FromSeconds(secondsToLetTheTaskRun));

                Console.WriteLine("Quitting....");
            }

            Console.WriteLine("Done!");
        }

        [Test]
        public async Task DoesNotDieOnTransientErrors()
        {
            var throwException = true;
            var taskWasCompleted = false;

            using (var task = new TplAsyncTask("bimse", async () =>
            {
                if (throwException)
                {
                    throw new Exception("but you told me to do it!");
                }

                taskWasCompleted = true;
            }, new ConsoleLoggerFactory(false))
            {
                Interval = TimeSpan.FromMilliseconds(400)
            })
            {
                Console.WriteLine("Starting the task...");
                task.Start();

                Console.WriteLine("Waiting for task to run a little...");
                await Task.Delay(TimeSpan.FromSeconds(1));

                Console.WriteLine("Suddenly, the transient error disappears...");
                throwException = false;

                Console.WriteLine("and life goes on...");
                await Task.Delay(TimeSpan.FromSeconds(1));

                Assert.That(taskWasCompleted, Is.True, "The task did NOT resume properly after experiencing exceptions!");
            }
        }

        [Test]
        public async Task ItWorks()
        {
            var stopwatch = Stopwatch.StartNew();
            var events = new ConcurrentQueue<TimeSpan>();
            var task = new TplAsyncTask("test task",
                async () =>
                {
                    events.Enqueue(stopwatch.Elapsed);
                },
                new ConsoleLoggerFactory(false))
            {
                Interval = TimeSpan.FromSeconds(0.2)
            };

            using (task)
            {
                task.Start();

                await Task.Delay(1199);
            }

            Console.WriteLine(string.Join(Environment.NewLine, events));

            Assert.That(events.Count, Is.GreaterThanOrEqualTo(5));
            Assert.That(events.Count, Is.LessThanOrEqualTo(7));
        }
    }
}