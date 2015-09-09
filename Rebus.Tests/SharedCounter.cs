using System;
using System.Threading;
using NUnit.Framework;
using Rebus.Tests.Extensions;
using Timer = System.Timers.Timer;

namespace Rebus.Tests
{
    /// <summary>
    /// Shared counter that can be used across threads to perform some specific number of actions
    /// </summary>
    public class SharedCounter : IDisposable
    {
        readonly Timer _statusTimer = new Timer(1000);
        readonly ManualResetEvent _resetEvent = new ManualResetEvent(false);
        readonly string _name;
        readonly int _initialValue;
        int _counter;
        bool _failure;
        string _failureText;

        public SharedCounter(int initialValue, string name = null)
        {
            _name = name ?? "<noname>";
            _initialValue = initialValue;
            _counter = initialValue;

            Console.WriteLine("Counter '{0}' initialized to {1}", _name, initialValue);

            _statusTimer.Elapsed += (o, ea) => Console.WriteLine("Counter '{0}': {1} ({2})", _name, _counter, _initialValue);
            _statusTimer.Start();
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
                    _resetEvent.Set();
                }
                else
                {
                    Console.WriteLine("Counter '{0}' reached 0 - setting reset event in {1}!", _name, Delay);

                    ThreadPool.QueueUserWorkItem(_ =>
                    {
                        Thread.Sleep(Delay);
                        _resetEvent.Set();
                    });
                }
            }
        }

        public ManualResetEvent ResetEvent
        {
            get { return _resetEvent; }
        }

        public void Dispose()
        {
            _statusTimer.Dispose();
        }

        public void WaitForResetEvent(int timeoutSeconds = 5)
        {
            var errorMessage = string.Format("Reset event for shared counter '{0}' was not set within {1} second timeout!",
                _name, timeoutSeconds);

            ResetEvent.WaitOrDie(TimeSpan.FromSeconds(timeoutSeconds), errorMessage);

            if (_failure)
            {
                throw new AssertionException(_failureText);
            }
        }
    }
}