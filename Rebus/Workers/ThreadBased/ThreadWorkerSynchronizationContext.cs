using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace Rebus.Workers.ThreadBased
{
    /// <summary>
    /// Derivation of <see cref="SynchronizationContext"/> that queues posted callbacks, allowing for worker threads to retrieve them later 
    /// on as a simple, callable <see cref="Action"/>, by calling <see cref="GetNextContinuationOrNull"/>
    /// </summary>
    public class ThreadWorkerSynchronizationContext : SynchronizationContext
    {
        readonly ConcurrentQueue<Tuple<SendOrPostCallback, object>> _callbacks =
            new ConcurrentQueue<Tuple<SendOrPostCallback, object>>();

        /// <summary>
        /// This method is called when a <see cref="Task"/> has finished and is ready to be continued
        /// </summary>
        public override void Post(SendOrPostCallback callback, object state)
        {
            _callbacks.Enqueue(Tuple.Create(callback, state));
        }

        /// <summary>
        /// Gets the next ready continuation if any, returns null otherwise
        /// </summary>
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