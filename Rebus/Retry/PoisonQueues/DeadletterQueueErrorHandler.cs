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
    public async Task HandlePoisonMessage(TransportMessage transportMessage, ITransactionContext transactionContext, Exception exception)
    {
        var headers = transportMessage.Headers;

        if (!headers.TryGetValue(Headers.MessageId, out var messageId))
        {
            messageId = "<unknown>";
        }

        headers[Headers.ErrorDetails] = GetErrorDetails(exception);
        headers[Headers.SourceQueue] = _transport.Address;

        var errorQueueAddress = _retryStrategySettings.ErrorQueueName;

        try
        {
            _log.Error(exception, "Moving message with ID {messageId} to error queue {queueName}", messageId, errorQueueAddress);

            await _transport.Send(errorQueueAddress, transportMessage, transactionContext);
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

    string GetErrorDetails(Exception exception)
    {
        var maxLength = _retryStrategySettings.ErrorDetailsHeaderMaxLength;

        // if there's not even room for the placeholder, just cut the crap
        if (maxLength < 5)
        {
            return "";
        }

        var errorDetails = exception.ToString().Truncate(maxLength);

        return errorDetails;
    }

}