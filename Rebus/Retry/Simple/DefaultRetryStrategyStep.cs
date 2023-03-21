using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Rebus.Bus;
using Rebus.Exceptions;
using Rebus.Extensions;
using Rebus.Logging;
using Rebus.Messages;
using Rebus.Pipeline;
using Rebus.Retry.FailFast;
using Rebus.Transport;

namespace Rebus.Retry.Simple;

/// <summary>
/// Incoming message pipeline step that implements a retry mechanism - if the call to the rest of the pipeline fails,
/// the exception is caught and the queue transaction is rolled back. Caught exceptions are tracked with <see cref="IErrorTracker"/>, and after
/// a configurable number of retries, the message will be passed to the configured <see cref="IErrorHandler"/>.
/// </summary>
[StepDocumentation(@"Wraps the invocation of the entire receive pipeline in an exception handler, tracking the number of times the received message has been attempted to be delivered.

If the maximum number of delivery attempts is reached, the message is passed to the error handler, which by default will move the message to the error queue.")]
public class DefaultRetryStrategyStep : IRetryStrategyStep
{
    /// <summary>
    /// Key of a step context item that indicates that the message must be wrapped in a <see cref="FailedMessageWrapper{TMessage}"/> after being deserialized
    /// </summary>
    public const string DispatchAsFailedMessageKey = "dispatch-as-failed-message";

    readonly CancellationToken _cancellationToken;
    readonly IErrorHandler _errorHandler;
    readonly IErrorTracker _errorTracker;
    readonly IFailFastChecker _failFastChecker;
    readonly bool _secondLevelRetriesEnabled;
    readonly ILog _logger;

    /// <summary>
    /// Creates the step
    /// </summary>
    public DefaultRetryStrategyStep(IRebusLoggerFactory rebusLoggerFactory, IErrorHandler errorHandler, IErrorTracker errorTracker, IFailFastChecker failFastChecker, bool secondLevelRetriesEnabled, CancellationToken cancellationToken)
    {
        _logger = rebusLoggerFactory?.GetLogger<DefaultRetryStrategyStep>() ?? throw new ArgumentNullException(nameof(rebusLoggerFactory));
        _errorHandler = errorHandler ?? throw new ArgumentNullException(nameof(errorHandler));
        _errorTracker = errorTracker ?? throw new ArgumentNullException(nameof(errorTracker));
        _failFastChecker = failFastChecker ?? throw new ArgumentNullException(nameof(failFastChecker));
        _secondLevelRetriesEnabled = secondLevelRetriesEnabled;
        _cancellationToken = cancellationToken;
    }

    /// <summary>
    /// Executes the entire message processing pipeline in an exception handler, tracking the number of failed delivery using <see cref="IErrorTracker"/>.
    /// Passes the message to the <see cref="IErrorHandler"/> when the max number of delivery attempts has been exceeded.
    /// </summary>
    public async Task Process(IncomingStepContext context, Func<Task> next)
    {
        var transactionContext = context.Load<ITransactionContext>() ?? throw new RebusApplicationException("Could not find a transaction context in the current incoming step context");
        var transportMessage = context.Load<TransportMessage>() ?? throw new RebusApplicationException("Could not find a transport message in the current incoming step context");
        var messageId = transportMessage.Headers.GetValueOrNull(Headers.MessageId);

        if (string.IsNullOrWhiteSpace(messageId))
        {
            transactionContext.SkipCommit();

            await PassToErrorHandler(context, new RebusApplicationException(
                $"Received message with empty or absent '{Headers.MessageId}' header! All messages must carry" +
                " an ID. If no ID is present, the message cannot be tracked" +
                " between delivery attempts, and other stuff would also be much harder to" +
                " do - therefore, it is a requirement that messages carry an ID."));

            return;
        }

        try
        {
            await next();

            var manualDeadletterCommand = context.Load<ManualDeadletterCommand>();

            if (manualDeadletterCommand != null)
            {
                await PassToErrorHandler(context, manualDeadletterCommand.Exception);
            }

            await transactionContext.Commit();
        }
        catch (OperationCanceledException) when (_cancellationToken.IsCancellationRequested)
        {
            _logger.Info("Dispatch of message with ID {messageId} was cancelled", messageId);
            transactionContext.Abort();
        }
        catch (Exception exception)
        {
            await HandleException(exception, transactionContext, messageId, context, next);
        }
    }

    async Task HandleException(Exception exception, ITransactionContext transactionContext, string messageId, IncomingStepContext context, Func<Task> next)
    {
        if (_failFastChecker.ShouldFailFast(messageId, exception))
        {
            await _errorTracker.MarkAsFinal(messageId);
            await _errorTracker.RegisterError(messageId, exception);
            await PassToErrorHandler(context, GetAggregateException(new[] { exception }));
            await _errorTracker.CleanUp(messageId);
            transactionContext.SkipCommit();
            return;
        }

        await _errorTracker.RegisterError(messageId, exception);

        if (!await _errorTracker.HasFailedTooManyTimes(messageId))
        {
            transactionContext.Abort();
            return;
        }

        if (_secondLevelRetriesEnabled)
        {
            try
            {
                await DispatchSecondLevelRetry(messageId, transactionContext, context, next);
                await transactionContext.Commit();
                await _errorTracker.CleanUp(messageId);
                return;
            }
            catch (OperationCanceledException) when (_cancellationToken.IsCancellationRequested)
            {
                _logger.Info("Dispatch of message with ID {messageId} was cancelled", messageId);
            }
            catch (Exception secondLevelException)
            {
                var exceptions = await _errorTracker.GetExceptions(messageId);
                await PassToErrorHandler(context, GetAggregateException(exceptions.Concat(new[] { secondLevelException })));
                await _errorTracker.CleanUp(messageId);
                transactionContext.SkipCommit();
                return;
            }
        }

        var aggregateException = GetAggregateException(await _errorTracker.GetExceptions(messageId));

        await PassToErrorHandler(context, aggregateException);
        await _errorTracker.CleanUp(messageId);
        transactionContext.SkipCommit();
    }

    static async Task DispatchSecondLevelRetry(string messageId, ITransactionContext transactionContext, IncomingStepContext context, Func<Task> next)
    {
        if (transactionContext.Items.TryGetValue(AbstractRebusTransport.OutgoingMessagesKey, out var result)
            && result is ConcurrentQueue<AbstractRebusTransport.OutgoingMessage> outgoingMessages)
        {
            outgoingMessages.Clear();
        }

        context.Save(DispatchAsFailedMessageKey, true);

        await next();
    }

    async Task PassToErrorHandler(StepContext context, Exception exception)
    {
        var originalTransportMessage = context.Load<OriginalTransportMessage>() ?? throw new RebusApplicationException("Could not find the original transport message in the current incoming step context");
        var transportMessage = originalTransportMessage.TransportMessage.Clone();

        using var scope = new RebusTransactionScope();
        await _errorHandler.HandlePoisonMessage(transportMessage, scope.TransactionContext, exception);
        await scope.CompleteAsync();
    }

    static AggregateException GetAggregateException(IEnumerable<Exception> exceptions)
    {
        var list = exceptions.ToList();

        return new AggregateException($"{list.Count} unhandled exceptions", list);
    }
}
