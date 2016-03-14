using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Rebus.Pipeline;
using Rebus.Sagas;
using Rebus.Transport;

namespace Rebus.Testing.Internals
{
    class LockStepper<TSagaHandler> : IIncomingStep where TSagaHandler : Saga
    {
        readonly ConcurrentQueue<ManualResetEvent> _resetEvents = new ConcurrentQueue<ManualResetEvent>();

        public void AddResetEvent(ManualResetEvent reserEvent)
        {
            _resetEvents.Enqueue(reserEvent);
        }

        public async Task Process(IncomingStepContext context, Func<Task> next)
        {
            context.Load<ITransactionContext>()
                .OnDisposed(() =>
                {
                    ManualResetEvent resetEvent;
                    while (_resetEvents.TryDequeue(out resetEvent))
                    {
                        resetEvent.Set();
                    }
                });

            await next();
        }
    }
}