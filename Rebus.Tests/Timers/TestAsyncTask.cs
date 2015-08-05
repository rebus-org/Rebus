using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading.Tasks;
using NUnit.Framework;
using Rebus.Threading;

namespace Rebus.Tests.Timers
{
    [TestFixture]
    public class TestAsyncTask : FixtureBase
    {
        [Test]
        public async Task DoesNotDieOnTransientErrors()
        {
            var throwException = true;
            var taskWasCompleted = false;
            
            using (var task = new AsyncTask("bimse", async () =>
            {
                if (throwException)
                {
                    throw new Exception("but you told me to do it!");
                }

                taskWasCompleted = true;
            })
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
            var task = new AsyncTask("test task",
                async () =>
                {
                    events.Enqueue(stopwatch.Elapsed);
                })
            {
                Interval = TimeSpan.FromSeconds(0.2)
            };

            using (task)
            {
                task.Start();

                await Task.Delay(1199);
            }

            await Task.Delay(TimeSpan.FromSeconds(0.7));

            Console.WriteLine(string.Join(Environment.NewLine, events));

            Assert.That(events.Count, Is.GreaterThanOrEqualTo(5));
            Assert.That(events.Count, Is.LessThanOrEqualTo(7));
        }
    }
}