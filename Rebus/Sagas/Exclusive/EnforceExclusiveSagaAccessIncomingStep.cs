using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Rebus.Bus;
using Rebus.Messages;
using Rebus.Pipeline;
using Rebus.Pipeline.Receive;
using Rebus.Transport;
#pragma warning disable 1998

namespace Rebus.Sagas.Exclusive
{
    class EnforceExclusiveSagaAccessIncomingStep : IIncomingStep
    {
        readonly ConcurrentDictionary<string, string> _locks = new ConcurrentDictionary<string, string>();
        readonly SagaHelper _sagaHelper = new SagaHelper();

        public async Task Process(IncomingStepContext context, Func<Task> next)
        {
            var handlerInvokersForSagas = context.Load<HandlerInvokers>()
                .Where(l => l.HasSaga)
                .ToList();

            if (!handlerInvokersForSagas.Any())
            {
                await next().ConfigureAwait(false);
                return;
            }

            var message = context.Load<Message>();
            var transactionContext = context.Load<ITransactionContext>();
            var messageContext = new MessageContext(transactionContext);

            var messageBody = message.Body;

            var correlationProperties = handlerInvokersForSagas
                .Select(h => h.Saga)
                .SelectMany(saga => _sagaHelper.GetCorrelationProperties(messageBody, saga).ForMessage(messageBody)
                    .Select(correlationProperty => new {saga, correlationProperty}))
                    .ToList();

            var locksToObtain = correlationProperties
                .Select(a => new
                {
                    SagaDataType = a.saga.GetSagaDataType().FullName,
                    CorrelationPropertyName = a.correlationProperty.PropertyName,
                    CorrelationPropertyValue = a.correlationProperty.ValueFromMessage(messageContext, messageBody)
                })
                .Select(a => a.ToString())
                .OrderBy(str => str) // enforce consistent ordering to avoid deadlocks
                .ToList();

            try
            {
                await WaitForLocks(locksToObtain, message.GetMessageId()).ConfigureAwait(false);
                await next().ConfigureAwait(false);
            }
            finally
            {
                await ReleaseLocks(locksToObtain).ConfigureAwait(false);
            }
        }

        async Task WaitForLocks(List<string> lockIds, string messageId)
        {
            foreach (var id in lockIds)
            {
                while (!_locks.TryAdd(id, messageId))
                {
                    await Task.Yield();
                }
            }
        }

        async Task ReleaseLocks(List<string> lockIds)
        {
            foreach (var lockId in lockIds)
            {
                _locks.TryRemove(lockId, out var dummy);
            }
        }

        public override string ToString() => "EnforceExclusiveSagaAccessIncomingStep";
    }
}