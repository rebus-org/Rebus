using System;
using System.Threading.Tasks;
using Rebus.Bus;
using Rebus.Logging;
using Rebus.Messages;
using Rebus.Retry.Simple;
using Rebus.Transport;

namespace Rebus.Retry.PoisonQueues
{
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
            if (simpleRetryStrategySettings == null) throw new ArgumentNullException(nameof(simpleRetryStrategySettings));
            if (transport == null) throw new ArgumentNullException(nameof(transport));
            if (rebusLoggerFactory == null) throw new ArgumentNullException(nameof(rebusLoggerFactory));
            _simpleRetryStrategySettings = simpleRetryStrategySettings;
            _transport = transport;
            _log = rebusLoggerFactory.GetCurrentClassLogger();
        }

        /// <summary>
        /// Initializes the poison queue error handler by creating the error queue if necessary
        /// </summary>
        public void Initialize()
        {
            _transport.CreateQueue(_simpleRetryStrategySettings.ErrorQueueAddress);
        }

        /// <summary>
        /// Handles the poisonous message by forwarding it to the configured error queue
        /// </summary>
        public async Task HandlePoisonMessage(TransportMessage transportMessage, ITransactionContext transactionContext, string errorDescription)
        {
            var headers = transportMessage.Headers;

            string messageId ;

            if (!headers.TryGetValue(Headers.MessageId, out messageId))
            {
                messageId = "<unknown>";
            }

            headers[Headers.ErrorDetails] = errorDescription;
            headers[Headers.SourceQueue] = _transport.Address;

            var errorQueueAddress = _simpleRetryStrategySettings.ErrorQueueAddress;

            try
            {
                _log.Error("Moving message with ID {0} to error queue '{1}' - reason: {2}", messageId, errorQueueAddress, errorDescription);

                await _transport.Send(errorQueueAddress, transportMessage, transactionContext);
            }
            catch (Exception exception)
            {
                _log.Error(exception, "Could not move message with ID {0} to error queue '{1}' - will pause {2} to avoid thrashing",
                    messageId, errorQueueAddress, MoveToErrorQueueFailedPause);

                // if we can't move to error queue, we need to avoid thrashing over and over
                await Task.Delay(MoveToErrorQueueFailedPause);
            }
        }
    }
}