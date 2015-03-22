using System;
using System.Collections.Concurrent;
using System.Threading;

namespace Rebus.Bus
{
    public class ThreadWorkerSynchronizationContext : SynchronizationContext
    {
        readonly ConcurrentQueue<Tuple<SendOrPostCallback, object>> _callbacks =
            new ConcurrentQueue<Tuple<SendOrPostCallback, object>>();

        public override void Post(SendOrPostCallback d, object state)
        {
            _callbacks.Enqueue(Tuple.Create(d, state));
        }

        internal Action GetNextContinuationOrNull()
        {
            Tuple<SendOrPostCallback, object> tuple;

            if (_callbacks.TryDequeue(out tuple))
                return () =>
                {
                    tuple.Item1(tuple.Item2);
                };

            return null;
        }
    }
}