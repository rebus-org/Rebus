using System;
using System.Threading.Tasks;
using Rebus.Bus;
using Rebus.Extensions;
using Rebus.Logging;
using Rebus.Messages;
using Rebus.Retry.Simple;
using Rebus.Transport;

namespace Rebus.Retry.PoisonQueues;

/// <summary>
/// Default <see cref="IErrorHandler"/> that uses a "poison queue" to function as storage for failed messages.
/// </summary>
public class DeadletterQueueErrorHandler : IErrorHandler, IInitializable
{
    readonly RetryStrategySettings _retryStrategySettings;
    readonly ITransport _transport;
    readonly ILog _log;

    /// <summary>
    /// Creates the error handler
    /// </summary>
    public DeadletterQueueErrorHandler(RetryStrategySettings retryStrategySettings, ITransport transport, IRebusLoggerFactory rebusLoggerFactory)
    {
        _log = rebusLoggerFactory?.GetLogger<DeadletterQueueErrorHandler>() ?? throw new ArgumentNullException(nameof(rebusLoggerFactory));
        _transport = transport ?? throw new ArgumentNullException(nameof(transport));
        _retryStrategySettings = retryStrategySettings ?? throw new ArgumentNullException(nameof(retryStrategySettings));
    }

    /// <summary>
    /// Initializes the poison queue error handler by creating the error queue if necessary
    /// </summary>
    public void Initialize()
    {
        var errorQueueAddress = _retryStrategySettings.ErrorQueueName;

        _transport.CreateQueue(errorQueueAddress);
    }

    /// <summary>
    /// Handles the poisonous message by forwarding it to the configured error queue
    /// </summary>
    public async Task HandlePoisonMessage(TransportMessage transportMessage, ITransactionContext transactionContext, ExceptionInfo exception)
    {
        // when errors are handled in IMMEDIATE mode, we need a separate transaction scope here - otherwise, we don't
        using var scope = _retryStrategySettings.ErrorHandlerMode == ErrorHandlerMode.Immediately ? new RebusTransactionScope() : null;

        transportMessage = transportMessage.Clone();

        var headers = transportMessage.Headers;

        if (!headers.TryGetValue(Headers.MessageId, out var messageId))
        {
            messageId = "<unknown>";
        }

        var errorDetails = GetErrorDetails(exception);

        headers[Headers.ErrorDetails] = errorDetails;
        headers[Headers.SourceQueue] = _transport.Address;

        var errorQueueAddress = _retryStrategySettings.ErrorQueueName;

        try
        {
            _log.Error("Moving message with ID {messageId} to error queue {queueName} - error details: {errorDetails}",
                messageId, errorQueueAddress, errorDetails);

            await _transport.Send(errorQueueAddress, transportMessage, scope?.TransactionContext ?? transactionContext);

            if (scope != null)
            {
                await scope.CompleteAsync();
            }
        }
        catch (Exception forwardException)
        {
            var cooldownTime = TimeSpan.FromSeconds(_retryStrategySettings.ErrorQueueErrorCooldownTimeSeconds);

            _log.Warn(forwardException, "Could not move message with ID {messageId} to error queue {queueName} - will pause {pauseInterval} to avoid thrashing",
                messageId, errorQueueAddress, cooldownTime);

            // if we can't move to error queue, we need to avoid thrashing over and over
            await Task.Delay(cooldownTime);
            throw;
        }
    }

    string GetErrorDetails(ExceptionInfo exception)
    {
        var maxLength = _retryStrategySettings.ErrorDetailsHeaderMaxLength;

        // if there's not even room for the placeholder, just cut the crap
        if (maxLength < 5)
        {
            return "";
        }

        var errorDetails = $"{exception.Message}: {exception.Details}";

        return errorDetails.Truncate(maxLength);
    }

}