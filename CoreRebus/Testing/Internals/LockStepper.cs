using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Rebus.Pipeline;
using Rebus.Transport;

namespace Rebus.Testing.Internals
{
    /// <summary>
    /// Pipeline step that makes it easy to block message processing until some particular point in time.
    /// This is done by adding a <see cref="ManualResetEvent"/> to it, which will be set the next time
    /// a message has been processed
    /// </summary>
    class LockStepper : IIncomingStep
    {
        readonly ConcurrentQueue<ManualResetEvent> _resetEvents = new ConcurrentQueue<ManualResetEvent>();

        public void AddResetEvent(ManualResetEvent reserEvent)
        {
            _resetEvents.Enqueue(reserEvent);
        }

        public async Task Process(IncomingStepContext context, Func<Task> next)
        {
            var resetEvents = DequeueResetEvents();

            context.Load<ITransactionContext>()
                .OnDisposed(() =>
                {
                    resetEvents.ForEach(resetEvent => resetEvent.Set());
                });

            await next();
        }

        List<ManualResetEvent> DequeueResetEvents()
        {
            var list = new List<ManualResetEvent>();

            ManualResetEvent resetEvent;
            while (_resetEvents.TryDequeue(out resetEvent))
            {
                list.Add(resetEvent);
            }

            return list;
        }
    }
}