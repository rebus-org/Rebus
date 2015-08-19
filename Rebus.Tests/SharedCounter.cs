using System;
using System.Threading;
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

        public SharedCounter(int initialValue, string name = null)
        {
            _name = name ?? "<noname>";
            _initialValue = initialValue;
            _counter = initialValue;

            Console.WriteLine("Counter '{0}' initialized to {1}", _name, initialValue);

            _statusTimer.Elapsed += (o, ea) => Console.WriteLine("Counter '{0}': {1} ({2})", _name, _counter, _initialValue);
            _statusTimer.Start();
        }

        public void Decrement()
        {
            var newValue = Interlocked.Decrement(ref _counter);

            if (newValue == 0)
            {
                Console.WriteLine("Counter '{0}' reached 0!", _name);
                _resetEvent.Set();
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
    }
}