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
public class PoisonQueueErrorHandler : IErrorHandler, IInitializable
{
    static readonly TimeSpan MoveToErrorQueueFailedPause = TimeSpan.FromSeconds(5);

    readonly SimpleRetryStrategySettings _simpleRetryStrategySettings;
    readonly ITransport _transport;
    readonly ILog _log;

    /// <summary>
    /// Creates the error handler
    /// </summary>
    public PoisonQueueErrorHandler(SimpleRetryStrategySettings simpleRetryStrategySettings, ITransport transport, IRebusLoggerFactory rebusLoggerFactory)
    {
        _log = rebusLoggerFactory?.GetLogger<PoisonQueueErrorHandler>() ?? throw new ArgumentNullException(nameof(rebusLoggerFactory));
        _transport = transport ?? throw new ArgumentNullException(nameof(transport));
        _simpleRetryStrategySettings = simpleRetryStrategySettings ?? throw new ArgumentNullException(nameof(simpleRetryStrategySettings));
    }

    /// <summary>
    /// Initializes the poison queue error handler by creating the error queue if necessary
    /// </summary>
    public void Initialize()
    {
        var errorQueueAddress = _simpleRetryStrategySettings.ErrorQueueAddress;

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

        var errorQueueAddress = _simpleRetryStrategySettings.ErrorQueueAddress;

        try
        {
            _log.Error(exception, "Moving message with ID {messageId} to error queue {queueName}", messageId, errorQueueAddress);

            await _transport.Send(errorQueueAddress, transportMessage, transactionContext);
        }
        catch (Exception forwardException)
        {
            _log.Error(forwardException, "Could not move message with ID {messageId} to error queue {queueName} - will pause {pauseInterval} to avoid thrashing",
                messageId, errorQueueAddress, MoveToErrorQueueFailedPause);

            // if we can't move to error queue, we need to avoid thrashing over and over
            await Task.Delay(MoveToErrorQueueFailedPause);
        }
    }

    string GetErrorDetails(Exception exception)
    {
        var maxLength = _simpleRetryStrategySettings.ErrorDetailsHeaderMaxLength;

        // if there's not even room for the placeholder, just cut the crap
        if (maxLength < 5)
        {
            return "";
        }

        var errorDetails = exception.ToString().Truncate(maxLength);

        return errorDetails;
    }

}