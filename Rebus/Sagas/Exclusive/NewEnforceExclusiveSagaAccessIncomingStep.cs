using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Rebus.Messages;
using Rebus.Pipeline;
using Rebus.Pipeline.Receive;
using Rebus.Transport;
// ReSharper disable ForCanBeConvertedToForeach

namespace Rebus.Sagas.Exclusive
{
    class NewEnforceExclusiveSagaAccessIncomingStep : IIncomingStep, IDisposable
    {
        readonly CancellationToken _cancellationToken;
        readonly SagaHelper _sagaHelper = new SagaHelper();
        readonly SemaphoreSlim[] _locks;

        public NewEnforceExclusiveSagaAccessIncomingStep(int lockBuckets, CancellationToken cancellationToken)
        {
            _cancellationToken = cancellationToken;
            _locks = Enumerable.Range(0, lockBuckets)
                .Select(n => new SemaphoreSlim(1, 1))
                .ToArray();
        }

        public async Task Process(IncomingStepContext context, Func<Task> next)
        {
            var handlerInvokersForSagas = context.Load<HandlerInvokers>()
                .Where(l => l.HasSaga)
                .ToList();

            if (!handlerInvokersForSagas.Any())
            {
                await next();
                return;
            }

            var message = context.Load<Message>();
            var transactionContext = context.Load<ITransactionContext>();
            var messageContext = new MessageContext(transactionContext);

            var messageBody = message.Body;

            var correlationProperties = handlerInvokersForSagas
                .Select(h => h.Saga)
                .SelectMany(saga => _sagaHelper.GetCorrelationProperties(messageBody, saga).ForMessage(messageBody)
                    .Select(correlationProperty => new { saga, correlationProperty }))
                    .ToList();

            var locksToObtain = correlationProperties
                .Select(a => new
                {
                    SagaDataType = a.saga.GetSagaDataType().FullName,
                    CorrelationPropertyName = a.correlationProperty.PropertyName,
                    CorrelationPropertyValue = a.correlationProperty.ValueFromMessage(messageContext, messageBody)
                })
                .Select(a => a.ToString())
                .Select(lockId => Math.Abs(lockId.GetHashCode()) % _locks.Length)
                .OrderBy(bucket => bucket) // enforce consistent ordering to avoid deadlocks
                .ToList();

            try
            {
                await WaitForLocks(locksToObtain);
                await next();
            }
            finally
            {
                ReleaseLocks(locksToObtain);
            }
        }

        async Task WaitForLocks(List<int> lockIds)
        {
            for (var index = 0; index < lockIds.Count; index++)
            {
                var id = lockIds[index];
                await _locks[id].WaitAsync(_cancellationToken);
            }
        }

        void ReleaseLocks(List<int> lockIds)
        {
            for (var index = 0; index < lockIds.Count; index++)
            {
                var id = lockIds[index];
                _locks[id].Release();
            }
        }

        public override string ToString()
        {
            return $"NewEnforceExclusiveSagaAccessIncomingStep({_locks.Length})";
        }

        bool _disposed;

        public void Dispose()
        {
            if (_disposed) return;

            try
            {
                foreach (var disposable in _locks)
                {
                    disposable.Dispose();
                }
            }
            finally
            {
                _disposed = true;
            }
        }
    }
}