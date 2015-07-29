using System;
using System.Collections.Concurrent;
using System.Threading;

namespace Rebus.Bus
{
    internal class RebusSynchronizationContext : SynchronizationContext
    {
        readonly ConcurrentQueue<Tuple<SendOrPostCallback, object>> callbacks =
            new ConcurrentQueue<Tuple<SendOrPostCallback, object>>();

        public override void Post(SendOrPostCallback d, object state)
        {
            callbacks.Enqueue(Tuple.Create(d, state));
        }

        internal void Run()
        {
            Tuple<SendOrPostCallback, object> tuple;
            while (callbacks.TryDequeue(out tuple))
            {
                tuple.Item1(tuple.Item2);
            }
        }
    }
}