using System;
using System.Threading.Tasks;
using Rebus.Extensions;
using Rebus.Logging;
using Rebus.Messages;
using Rebus.Pipeline;
using Rebus.Transport;

namespace Rebus.Retry.Simple
{
    /// <summary>
    /// Incoming message pipeline step that implements a retry mechanism - if the call to the rest of the pipeline fails,
    /// the exception is caught and the queue transaction is rolled back. Caught exceptions are tracked in-mem, and after
    /// a configurable number of retries, the message will be forwarded to the configured error queue and the rest of the pipeline will not be called
    /// </summary>
    [StepDocumentation(@"Wraps the invocation of the entire receive pipeline in an exception handler, tracking the number of times the received message has been attempted to be delivered.

If the maximum number of delivery attempts is reached, the message is moved to the error queue.")]
    public class SimpleRetryStrategyStep : IRetryStrategyStep
    {
        /// <summary>
        /// Key of a step context item that indicates that the message must be wrapped in a <see cref="Failed{TMessage}"/> after being deserialized
        /// </summary>
        public const string DispatchAsFailedMessageKey = "dispatch-as-failed-message";
        
        static readonly TimeSpan MoveToErrorQueueFailedPause = TimeSpan.FromSeconds(5);
        static ILog _log;

        static SimpleRetryStrategyStep()
        {
            RebusLoggerFactory.Changed += f => _log = f.GetCurrentClassLogger();
        }

        readonly SimpleRetryStrategySettings _simpleRetryStrategySettings;
        readonly IErrorTracker _errorTracker;
        readonly ITransport _transport;

        /// <summary>
        /// Constructs the step, using the given transport and settings
        /// </summary>
        public SimpleRetryStrategyStep(ITransport transport, SimpleRetryStrategySettings simpleRetryStrategySettings, IErrorTracker errorTracker)
        {
            _transport = transport;
            _simpleRetryStrategySettings = simpleRetryStrategySettings;
            _errorTracker = errorTracker;
        }

        /// <summary>
        /// Executes the entire message processing pipeline in an exception handler, tracking the number of failed delivery attempts.
        /// Forwards the message to the error queue when the max number of delivery attempts has been exceeded.
        /// </summary>
        public async Task Process(IncomingStepContext context, Func<Task> next)
        {
            var transportMessage = context.Load<TransportMessage>();
            var transactionContext = context.Load<ITransactionContext>();
            var messageId = transportMessage.Headers.GetValueOrNull(Headers.MessageId);

            if (string.IsNullOrWhiteSpace(messageId))
            {
                await MoveMessageToErrorQueue("<no message ID>", transportMessage,
                    transactionContext, string.Format("Received message with empty or absent '{0}' header! All messages must be" +
                                                      " supplied with an ID . If no ID is present, the message cannot be tracked" +
                                                      " between delivery attempts, and other stuff would also be much harder to" +
                                                      " do - therefore, it is a requirement that messages be supplied with an ID.",
                        Headers.MessageId),
                        shortErrorDescription: string.Format("Received message with empty or absent '{0}' header", Headers.MessageId));

                return;
            }

            if (_errorTracker.HasFailedTooManyTimes(messageId))
            {
                // if we don't have 2nd level retries, just get the message out of the way
                if (!_simpleRetryStrategySettings.SecondLevelRetriesEnabled)
                {
                    await
                        MoveMessageToErrorQueue(messageId, transportMessage, transactionContext,
                            GetErrorDescriptionFor(messageId), GetErrorDescriptionFor(messageId, brief: true));

                    _errorTracker.CleanUp(messageId);
                    return;
                }

                // change the identifier to track by to perform this 2nd level of delivery attempts
                var secondLevelMessageId = messageId + "-2nd-level";

                if (_errorTracker.HasFailedTooManyTimes(secondLevelMessageId))
                {
                    await
                        MoveMessageToErrorQueue(messageId, transportMessage, transactionContext,
                            GetErrorDescriptionFor(messageId), GetErrorDescriptionFor(messageId, brief: true));

                    _errorTracker.CleanUp(messageId);
                    _errorTracker.CleanUp(secondLevelMessageId);
                    return;
                }

                context.Save(DispatchAsFailedMessageKey, true);

                await DispatchWithTrackerIdentifier(next, secondLevelMessageId, transactionContext);
                return;
            }

            await DispatchWithTrackerIdentifier(next, messageId, transactionContext);
        }

        async Task DispatchWithTrackerIdentifier(Func<Task> next, string identifierToTrackMessageBy, ITransactionContext transactionContext)
        {
            try
            {
                await next();
            }
            catch (Exception exception)
            {
                _errorTracker.RegisterError(identifierToTrackMessageBy, exception);

                transactionContext.Abort();
            }
        }

        string GetErrorDescriptionFor(string messageId, bool brief = false)
        {
            if (brief)
            {
                return _errorTracker.GetShortErrorDescription(messageId);
            }

            return _errorTracker.GetFullErrorDescription(messageId);
        }

        async Task MoveMessageToErrorQueue(string messageId, TransportMessage transportMessage, ITransactionContext transactionContext, string errorDescription, string shortErrorDescription = null)
        {
            var headers = transportMessage.Headers;

            headers[Headers.ErrorDetails] = errorDescription;
            headers[Headers.SourceQueue] = _transport.Address;

            var moveToErrorQueueFailed = false;
            var errorQueueAddress = _simpleRetryStrategySettings.ErrorQueueAddress;

            try
            {
                _log.Error("Moving message with ID {0} to error queue '{1}' - reason: {2}", messageId, errorQueueAddress, shortErrorDescription);

                await _transport.Send(errorQueueAddress, transportMessage, transactionContext);
            }
            catch (Exception exception)
            {
                _log.Error(exception, "Could not move message with ID {0} to error queue '{1}' - will pause {2} to avoid thrashing",
                    messageId, errorQueueAddress, MoveToErrorQueueFailedPause);

                moveToErrorQueueFailed = true;
            }

            // if we can't move to error queue, we need to avoid thrashing over and over
            if (moveToErrorQueueFailed)
            {
                await Task.Delay(MoveToErrorQueueFailedPause);
            }
        }
    }
}