using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Rebus.Bus.Advanced;

namespace Rebus.Tests.Assumptions;

[TestFixture]
public class TestAsyncWrapper
{
    public enum InvocationMode
    {
        Await,
        TaskRun,
        ThreadPool,
        ThreadPoolUnsafe,
        AsyncHelpers
    }
        
    [TestCase(InvocationMode.Await)]
    [TestCase(InvocationMode.TaskRun, Ignore = "slow")]
    [TestCase(InvocationMode.ThreadPool, Ignore = "slow")]
    [TestCase(InvocationMode.ThreadPoolUnsafe, Ignore = "slow")]
    [TestCase(InvocationMode.AsyncHelpers)]
    public async Task CompareInvocationSpeeds(InvocationMode invocationMode)
    {
        const int iterations = 100;

        var stopwatch = Stopwatch.StartNew();

        switch (invocationMode)
        {
            case InvocationMode.Await:
                for (var counter = 0; counter < iterations; counter++)
                {
                    await AsyncMethod();
                }
                break;
            case InvocationMode.TaskRun:
                for (var counter = 0; counter < iterations; counter++)
                {
                    Task.Run(AsyncMethod).Wait();
                }
                break;
            case InvocationMode.ThreadPool:
                for (var counter = 0; counter < iterations; counter++)
                {
                    var done = new ManualResetEvent(false);
                    ThreadPool.QueueUserWorkItem(_ => AsyncMethod().ContinueWith(t => done.Set()));
                    done.WaitOne();
                }
                break;
            case InvocationMode.ThreadPoolUnsafe:
                for (var counter = 0; counter < iterations; counter++)
                {
                    var done = new ManualResetEvent(false);
                    ThreadPool.UnsafeQueueUserWorkItem(_ => AsyncMethod().ContinueWith(t => done.Set()), null);
                    done.WaitOne();
                }
                break;
            case InvocationMode.AsyncHelpers:
                for (var counter = 0; counter < iterations; counter++)
                {
                    RebusAsyncHelpers.RunSync(AsyncMethod);
                }
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(invocationMode), invocationMode, null);
        }

        var elapsed = stopwatch.Elapsed;

        Console.WriteLine($"Invocation with mode = {invocationMode} took {elapsed.TotalMilliseconds:0} ms");
    }

    async Task AsyncMethod()
    {
        var veryShortNonZeroDelay = TimeSpan.FromMilliseconds(1);

        await Task.Delay(veryShortNonZeroDelay);
    }
}