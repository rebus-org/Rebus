using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Rebus.Messages;
using Rebus.Pipeline;
using Rebus.Pipeline.Receive;
using Rebus.Transport;
// ReSharper disable ForCanBeConvertedToForeach

namespace Rebus.Sagas.Exclusive;

abstract class EnforceExclusiveSagaAccessIncomingStepBase : IIncomingStep
{
    readonly SagaHelper _sagaHelper = new SagaHelper();
    protected readonly int _maxLockBuckets;
    protected readonly CancellationToken _cancellationToken;

    protected EnforceExclusiveSagaAccessIncomingStepBase(int maxLockBuckets, CancellationToken cancellationToken)
    {
        _maxLockBuckets = maxLockBuckets;
        _cancellationToken = cancellationToken;
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
        var body = message.Body;

        var correlationProperties = handlerInvokersForSagas
            .Select(h => h.Saga)
            .SelectMany(saga => _sagaHelper.GetCorrelationProperties(saga).ForMessage(body)
                .Select(correlationProperty => new { saga, correlationProperty }))
            .ToList();

        var locksToObtain = correlationProperties
            .Select(a => new
            {
                SagaDataType = a.saga.GetSagaDataType().FullName,
                CorrelationPropertyName = a.correlationProperty.PropertyName,
                CorrelationPropertyValue = a.correlationProperty.GetValueFromMessage(messageContext, message)
            })
            .Select(a => a.ToString())
            .Select(lockId => Math.Abs(lockId.GetHashCode()) % _maxLockBuckets)
            .Distinct() // avoid accidentally acquiring the same lock twice, because a bucket got hit more than once
            .OrderBy(str => str) // enforce consistent ordering to avoid deadlocks
            .ToArray();

        try
        {
            await WaitForLocks(locksToObtain);
            await next();
        }
        finally
        {
            await ReleaseLocks(locksToObtain);
        }
    }

    protected abstract Task<bool> AcquireLockAsync(int lockId);

    protected abstract Task<bool> ReleaseLockAsync(int lockId);

    async Task WaitForLocks(int[] lockIds)
    {
        for (var index = 0; index < lockIds.Length; index++)
        {
            while (!await AcquireLockAsync(lockIds[index]))
            {
                await Task.Yield();
            }
        }
    }

    async Task ReleaseLocks(int[] lockIds)
    {
        for (var index = 0; index < lockIds.Length; index++)
        {
            await ReleaseLockAsync(lockIds[index]);
        }
    }
}