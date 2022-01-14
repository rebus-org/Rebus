using System;
using System.Diagnostics;
using System.Threading.Tasks;
using NUnit.Framework;
using Rebus.Tests.Contracts;
using Rebus.Tests.Contracts.Utilities;
using Rebus.Tests.Timers.Factories;

#pragma warning disable 1998

namespace Rebus.Tests.Timers;

[TestFixture(typeof(ThreadingTimerTaskFactory))]
[TestFixture(typeof(TplTaskFactory))]
public class CompareAsyncTasks<TTaskFactory> : FixtureBase where TTaskFactory : IAsyncTaskFactory, new()
{
    TTaskFactory _factory;

    protected override void SetUp()
    {
        _factory = new TTaskFactory();
    }

    [Test]
    public async Task CheckTimerDrift()
    {
        const int testDurationSeconds = 10;

        var counter = new SharedCounter(testDurationSeconds);
        var stopwatch = Stopwatch.StartNew();

        var task = _factory.CreateTask(TimeSpan.FromSeconds(1), async () =>
        {
            var now = DateTime.Now;
            Console.WriteLine($"{now:mm:ss}-{now.Millisecond:000}");
            counter.Decrement();
        });

        using (task)
        {
            task.Start();

            counter.WaitForResetEvent(timeoutSeconds: 15);
        }

        var elapsed = stopwatch.Elapsed;
        var drift = elapsed - TimeSpan.FromSeconds(testDurationSeconds);

        Console.WriteLine($"Timer {task.GetType().Name} drifted {drift.TotalSeconds:0.000} s over duration {testDurationSeconds} s");
    }
}