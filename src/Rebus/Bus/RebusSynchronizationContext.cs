using System;
using System.Collections.Concurrent;
using System.Threading;

namespace Rebus.Bus
{
    internal class RebusSynchronizationContext : SynchronizationContext
    {
        readonly ConcurrentQueue<Tuple<SendOrPostCallback, object, ITransactionContext>> callbacks =
            new ConcurrentQueue<Tuple<SendOrPostCallback, object, ITransactionContext>>();

        public override void Post(SendOrPostCallback d, object state)
        {
            var txc = TransactionContext.Current;
            callbacks.Enqueue(Tuple.Create(d, state, txc));
        }

        internal void Run()
        {
            Tuple<SendOrPostCallback, object, ITransactionContext> tuple;
            while (callbacks.TryDequeue(out tuple))
            {
                TransactionContext.Set(tuple.Item3);
                tuple.Item1(tuple.Item2);
                TransactionContext.Clear();
            }
        }
    }
}