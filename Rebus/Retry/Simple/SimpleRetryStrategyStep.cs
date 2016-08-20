using System;
using System.Threading.Tasks;
using Rebus.Extensions;
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
        /// Key of a step context item that indicates that the message must be wrapped in a <see cref="FailedMessageWrapper{TMessage}"/> after being deserialized
        /// </summary>
        public const string DispatchAsFailedMessageKey = "dispatch-as-failed-message";

        readonly SimpleRetryStrategySettings _simpleRetryStrategySettings;
        readonly IErrorTracker _errorTracker;
        readonly IErrorHandler _errorHandler;

        /// <summary>
        /// Constructs the step, using the given transport and settings
        /// </summary>
        public SimpleRetryStrategyStep(SimpleRetryStrategySettings simpleRetryStrategySettings, IErrorTracker errorTracker, IErrorHandler errorHandler)
        {
            if (simpleRetryStrategySettings == null) throw new ArgumentNullException(nameof(simpleRetryStrategySettings));
            if (errorTracker == null) throw new ArgumentNullException(nameof(errorTracker));
            if (errorHandler == null) throw new ArgumentNullException(nameof(errorHandler));

            _simpleRetryStrategySettings = simpleRetryStrategySettings;
            _errorTracker = errorTracker;
            _errorHandler = errorHandler;
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
                await MoveMessageToErrorQueue(transportMessage, transactionContext,

                    $"Received message with empty or absent '{Headers.MessageId}' header! All messages must be" +
                    " supplied with an ID . If no ID is present, the message cannot be tracked" +
                    " between delivery attempts, and other stuff would also be much harder to" +
                    " do - therefore, it is a requirement that messages be supplied with an ID.");

                return;
            }

            if (_errorTracker.HasFailedTooManyTimes(messageId))
            {
                var errorDescriptionFor = GetErrorDescriptionFor(messageId);

                // if we don't have 2nd level retries, just get the message out of the way
                if (!_simpleRetryStrategySettings.SecondLevelRetriesEnabled)
                {
                    await MoveMessageToErrorQueue(transportMessage, transactionContext, errorDescriptionFor);
                    _errorTracker.CleanUp(messageId);
                    return;
                }

                // change the identifier to track by to perform this 2nd level of delivery attempts
                var secondLevelMessageId = GetSecondLevelMessageId(messageId);

                if (_errorTracker.HasFailedTooManyTimes(secondLevelMessageId))
                {
                    await MoveMessageToErrorQueue(transportMessage, transactionContext, errorDescriptionFor);
                    _errorTracker.CleanUp(messageId);
                    _errorTracker.CleanUp(secondLevelMessageId);
                    return;
                }

                context.Save(DispatchAsFailedMessageKey, true);

                await DispatchWithTrackerIdentifier(next, secondLevelMessageId, transactionContext, new[] { messageId, secondLevelMessageId });

                return;
            }

            await DispatchWithTrackerIdentifier(next, messageId, transactionContext, new[] { messageId });
        }

        static string GetSecondLevelMessageId(string messageId)
        {
            return messageId + "-2nd-level";
        }

        async Task DispatchWithTrackerIdentifier(Func<Task> next, string identifierToTrackMessageBy, ITransactionContext transactionContext, string[] identifiersToClearOnSuccess)
        {
            try
            {
                await next();

                await transactionContext.Commit();

                foreach (var id in identifiersToClearOnSuccess)
                {
                    _errorTracker.CleanUp(id);
                }
            }
            catch (Exception exception)
            {
                _errorTracker.RegisterError(identifierToTrackMessageBy, exception);

                transactionContext.Abort();
            }
        }

        string GetErrorDescriptionFor(string messageId, bool brief = false)
        {
            var secondLevelMessageId = GetSecondLevelMessageId(messageId);

            if (brief)
            {
                var secondLevelErrorDescription = _errorTracker.GetShortErrorDescription(secondLevelMessageId);
                var firstLevelErrorDescription = _errorTracker.GetShortErrorDescription(messageId);

                return secondLevelErrorDescription == null
                    ? firstLevelErrorDescription
                    : string.Join(Environment.NewLine, firstLevelErrorDescription, secondLevelErrorDescription);
            }
            else
            {
                var secondLevelErrorDescription = _errorTracker.GetFullErrorDescription(secondLevelMessageId);
                var firstLevelErrorDescription = _errorTracker.GetFullErrorDescription(messageId);

                return secondLevelErrorDescription == null
                    ? firstLevelErrorDescription
                    : string.Join(Environment.NewLine, firstLevelErrorDescription, secondLevelErrorDescription);
            }
        }

        async Task MoveMessageToErrorQueue(TransportMessage transportMessage, ITransactionContext transactionContext, string errorDescription)
        {
            await _errorHandler.HandlePoisonMessage(transportMessage, transactionContext, errorDescription);
        }
    }
}