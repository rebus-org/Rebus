using System;
using System.Collections.Concurrent;
using System.Threading;
using Rebus.Transport;

namespace Rebus.Bus
{
    public class ThreadWorkerSynchronizationContext : SynchronizationContext
    {
        readonly ConcurrentQueue<Tuple<SendOrPostCallback, object, ITransactionContext>> _callbacks =
            new ConcurrentQueue<Tuple<SendOrPostCallback, object, ITransactionContext>>();

        public override void Post(SendOrPostCallback d, object state)
        {
            var context = AmbientTransactionContext.Current;
            
            if (context == null)
            {
                Console.WriteLine("POST WITHOUT A TRANSACTION CONTEXT!");
                throw new InvalidOperationException("Attempted to Post without a transaction context, but that should not be possible!");
            }
            
            _callbacks.Enqueue(Tuple.Create(d, state, context));
        }

        internal Action GetNextContinuationOrNull()
        {
            Tuple<SendOrPostCallback, object, ITransactionContext> tuple;
            
            if (_callbacks.TryDequeue(out tuple))
                return () =>
                {
                    var context = tuple.Item3;
                    AmbientTransactionContext.Current = context;
                    try
                    {
                        tuple.Item1(tuple.Item2);
                    }
                    finally
                    {
                        AmbientTransactionContext.Current = null;
                    }
                };

            return null;
        }
    }
}