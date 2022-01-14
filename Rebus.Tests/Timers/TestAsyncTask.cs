using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Rebus.Tests.Contracts;
using Rebus.Tests.Timers.Factories;
// ReSharper disable ArgumentsStyleAnonymousFunction

#pragma warning disable 1998

namespace Rebus.Tests.Timers;

[TestFixture(typeof(TplTaskFactory))]
[TestFixture(typeof(ThreadingTimerTaskFactory))]
public class TestAsyncTask<TFactory> : FixtureBase where TFactory : IAsyncTaskFactory, new()
{
    TFactory _factory;

    protected override void SetUp()
    {
        _factory = new TFactory();
    }

    [Test]
    public async Task CanCancelTaskFromCancellationTokenSource()
    {
        var cancellationTokenSource = Using(new CancellationTokenSource());
        var calls = 0;

        var task = _factory.CreateTask(
            interval: TimeSpan.FromSeconds(1),
            action: async () =>
            {
                Console.WriteLine($"Task function called {DateTimeOffset.Now}");
                calls++;

                if (calls == 3)
                {
                    Console.WriteLine($"Got 3 calls now, cancelling task {DateTimeOffset.Now}");
                    cancellationTokenSource.Cancel();
                }
            }
        );

        task.Start();

        using (task)
        {
            await Task.Delay(TimeSpan.FromSeconds(4), CancellationToken.None);
        }
    }

    [Test]
    public async Task AsyncTaskMayBeStoppedBeforeBeingInvoked()
    {
        var wasInvoked = false;

        Console.WriteLine("Starting task...");

        using (_factory.CreateTask(TimeSpan.FromSeconds(2), async () => wasInvoked = true))
        {
            Console.WriteLine("Waiting one second...");

            await Task.Delay(TimeSpan.FromSeconds(1));

            Console.WriteLine("Stopping task...");
        }

        Assert.That(wasInvoked, Is.False);

        Console.WriteLine("Waiting two more seconds...");

        await Task.Delay(TimeSpan.FromSeconds(2));

        Assert.That(wasInvoked, Is.False);
    }

    [TestCase(0)]
    [TestCase(1)]
    [TestCase(2)]
    [TestCase(5)]
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

    [Test]
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

            Assert.That(taskWasCompleted, Is.True, "The task did NOT resume properly after experiencing exceptions!");
        }
    }

    [Test]
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

        Assert.That(events.Count, Is.GreaterThanOrEqualTo(3), "TPL-based tasks are wildly inaccurate and can sometimes add 2-300 ms per Task.Delay");
        Assert.That(events.Count, Is.LessThanOrEqualTo(8));
    }
}