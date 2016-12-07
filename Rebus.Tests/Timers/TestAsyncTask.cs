using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Rebus.Tests.Contracts;
using Rebus.Tests.Timers.Factories;
using Xunit;

#pragma warning disable 1998

namespace Rebus.Tests.Timers
{
    public class TestAsyncTaskForTplTaskFactory : TestAsyncTask<TplTaskFactory> {}
    public class TestAsyncTaskForTimerTaskFactory : TestAsyncTask<TimerTaskFactory> {}
    public class TestAsyncTaskForThreadingTimerTaskFactory : TestAsyncTask<ThreadingTimerTaskFactory> {}

    public abstract class TestAsyncTask<TFactory> : FixtureBase where TFactory : IAsyncTaskFactory, new()
    {
        readonly TFactory _factory;

        protected TestAsyncTask()
        {
            _factory = new TFactory();
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(5)]
        public async Task CanActuallyStopTaskWithLongInterval(int secondsToLetTheTaskRun)
        {
            var task = _factory.CreateTask(TimeSpan.FromMinutes(4.5), async () => { Console.WriteLine("INVOKED!!!"); });

            using (task)
            {
                task.Start();

                Console.WriteLine($"Letting the task run for {secondsToLetTheTaskRun} seconds...");

                await Task.Delay(TimeSpan.FromSeconds(secondsToLetTheTaskRun));

                Console.WriteLine("Quitting....");
            }

            Console.WriteLine("Done!");
        }

        [Fact]
        public async Task DoesNotDieOnTransientErrors()
        {
            var throwException = true;
            var taskWasCompleted = false;

            var task = _factory.CreateTask(TimeSpan.FromMilliseconds(400), async () =>
            {
                if (throwException)
                {
                    throw new Exception("but you told me to do it!");
                }

                taskWasCompleted = true;
            });

            using (task)
            {
                Console.WriteLine("Starting the task...");
                task.Start();

                Console.WriteLine("Waiting for task to run a little...");
                await Task.Delay(TimeSpan.FromSeconds(1));

                Console.WriteLine("Suddenly, the transient error disappears...");
                throwException = false;

                Console.WriteLine("and life goes on...");
                await Task.Delay(TimeSpan.FromSeconds(1));

                Assert.True(taskWasCompleted, "The task did NOT resume properly after experiencing exceptions!");
            }
        }

        [Fact]
        public async Task WorksWithSomeKindOfAccuracy()
        {
            var stopwatch = Stopwatch.StartNew();
            var events = new ConcurrentQueue<TimeSpan>();
            var task = _factory.CreateTask(TimeSpan.FromSeconds(0.2),
                async () =>
                {
                    events.Enqueue(stopwatch.Elapsed);
                });

            using (task)
            {
                task.Start();

                await Task.Delay(1199);
            }

            Console.WriteLine(string.Join(Environment.NewLine, events.Select(t => $"{t.TotalMilliseconds:0.0} ms")));

            Assert.True(events.Count >= 3, "TPL-based tasks are wildly inaccurate and can sometimes add 2-300 ms per Task.Delay");
            Assert.True(events.Count <= 8);
        }
    }
}