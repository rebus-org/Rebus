using System;
using System.Diagnostics;
using System.Threading;
using NUnit.Framework;
using Rebus.Tests.Contracts.Extensions;

namespace Rebus.Tests.Contracts.Utilities;

/// <summary>
/// Shared counter that can be used across threads to perform some specific number of actions
/// </summary>
public class SharedCounter : IDisposable
{
    Timer _statusTimer;
    readonly ManualResetEvent _resetEvent = new ManualResetEvent(false);
    readonly string _name;
    readonly int _initialValue;
    readonly Stopwatch _stopwatch;
    int _counter;
    bool _failure;
    string _failureText;

    public SharedCounter(int initialValue, string name = null)
    {
        _name = name ?? "<noname>";
        _initialValue = initialValue;
        _counter = initialValue;

        Console.WriteLine("Counter '{0}' initialized to {1}", _name, initialValue);

        _stopwatch = Stopwatch.StartNew();

        _statusTimer = new Timer((object o) => {
            Console.WriteLine("Counter '{0}' - value: {1} (initial: {2}, waited: {3:0.#} s)",
                _name, _counter, _initialValue, _stopwatch.Elapsed.TotalSeconds);
        }, null, TimeSpan.FromMilliseconds(1000), TimeSpan.FromMilliseconds(1000));
    }

    public TimeSpan Delay { get; set; }

    public void Fail(string message, params object[] objs)
    {
        _failure = true;
        _failureText = string.Format(message, objs);

        _resetEvent.Set();
    }

    public void Decrement()
    {
        var newValue = Interlocked.Decrement(ref _counter);

        if (newValue == 0)
        {
            if (Delay <= TimeSpan.FromSeconds(0))
            {
                Console.WriteLine("Counter '{0}' reached 0!", _name);
                Complete();
            }
            else
            {
                Console.WriteLine("Counter '{0}' reached 0 - setting reset event in {1}!", _name, Delay);

                ThreadPool.QueueUserWorkItem(_ =>
                {
                    Thread.Sleep(Delay);
                    Complete();
                });
            }
        }
    }

    void Complete()
    {
        _statusTimer?.Dispose();
        _statusTimer = null;

        Console.WriteLine("Counter '{0}' completed in {1:0.#} s", _name, _stopwatch.Elapsed.TotalSeconds);

        _resetEvent.Set();
    }

    public ManualResetEvent ResetEvent => _resetEvent;

    public void Dispose()
    {
        _statusTimer?.Dispose();
    }

    public void WaitForResetEvent(int timeoutSeconds = 5)
    {
        var errorMessage = $"Reset event for shared counter '{_name}' was not set within {timeoutSeconds} second timeout!";

        ResetEvent.WaitOrDie(TimeSpan.FromSeconds(timeoutSeconds), errorMessage);

        if (_failure)
        {
            throw new AssertionException(_failureText);
        }
    }
}