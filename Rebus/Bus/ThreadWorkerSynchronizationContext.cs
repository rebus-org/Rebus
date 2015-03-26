using System;
using System.Collections.Concurrent;
using System.Threading;

namespace Rebus.Bus
{
    /// <summary>
    /// Derivation of <see cref="SynchronizationContext"/> that queues posted callbacks, allowing for worker threads to retrieve them later 
    /// on as a simple, callable <see cref="Action"/>, by calling <see cref="GetNextContinuationOrNull"/>
    /// </summary>
    public class ThreadWorkerSynchronizationContext : SynchronizationContext
    {
        readonly ConcurrentQueue<Tuple<SendOrPostCallback, object>> _callbacks =
            new ConcurrentQueue<Tuple<SendOrPostCallback, object>>();

        public override void Post(SendOrPostCallback d, object state)
        {
            _callbacks.Enqueue(Tuple.Create(d, state));
        }

        public Action GetNextContinuationOrNull()
        {
            Tuple<SendOrPostCallback, object> tuple;
            if (!_callbacks.TryDequeue(out tuple)) return null;

            return () =>
            {
                tuple.Item1(tuple.Item2);
            };
        }
    }
}