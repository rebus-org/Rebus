using System;
using System.Threading;
using System.Threading.Tasks;
using Rebus.Bus;
using Rebus.Messages;
using Rebus.Pipeline;
using Rebus.Transport;

namespace Rebus.Retry.Simple;

/// <summary>
/// Incoming message pipeline step that implements a retry mechanism - if the call to the rest of the pipeline fails,
/// the exception is caught and the queue transaction is rolled back. Caught exceptions are tracked with <see cref="IErrorTracker"/>, and after
/// a configurable number of retries, the message will be passed to the configured <see cref="IErrorHandler"/>.
/// </summary>
[StepDocumentation(@"Wraps the invocation of the entire receive pipeline in an exception handler, tracking the number of times the received message has been attempted to be delivered.

If the maximum number of delivery attempts is reached, the message is passed to the error handler, which by default will move the message to the error queue.")]
public class DefaultRetryStrategyStep : IIncomingStep
{
    readonly IErrorHandler _errorHandler;
    readonly IErrorTracker _errorTracker;
    readonly CancellationToken _cancellationToken;

    /// <summary>
    /// Creates the step
    /// </summary>
    public DefaultRetryStrategyStep(IErrorHandler errorHandler, IErrorTracker errorTracker, CancellationToken cancellationToken)
    {
        _errorHandler = errorHandler ?? throw new ArgumentNullException(nameof(errorHandler));
        _errorTracker = errorTracker ?? throw new ArgumentNullException(nameof(errorTracker));
        _cancellationToken = cancellationToken;
    }

    public async Task Process(IncomingStepContext context, Func<Task> next)
    {
        var transactionContext = context.Load<ITransactionContext>();

        try
        {
            await next();

            await transactionContext.Commit();
        }
        catch (Exception exception)
        {
            var originalTransportMessage = context.Load<OriginalTransportMessage>();
            var transportMessage = originalTransportMessage.TransportMessage;

            var messageId = transportMessage.GetMessageId();
            await _errorTracker.RegisterError(messageId, exception);

            if (await _errorTracker.HasFailedTooManyTimes(messageId))
            {
                using var scope = new RebusTransactionScope();
                var exceptions = await _errorTracker.GetExceptions(messageId);
                await _errorHandler.HandlePoisonMessage(transportMessage, scope.TransactionContext, new AggregateException(exceptions));
                await scope.CompleteAsync();
                await _errorTracker.CleanUp(messageId);

                transactionContext.SkipCommit();
            }
            else
            {
                transactionContext.Abort();
            }
        }
    }
}
