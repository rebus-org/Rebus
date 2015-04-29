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

                await Task.Delay(1100);
            }

            await Task.Delay(TimeSpan.FromSeconds(0.7));

            Console.WriteLine(string.Join(Environment.NewLine, events));

            Assert.That(events.Count, Is.GreaterThanOrEqualTo(5));
            Assert.That(events.Count, Is.LessThanOrEqualTo(7));
        }
    }
}