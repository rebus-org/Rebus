using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Rebus.Bus;
using Rebus.Extensions;
using Rebus.Logging;
using Rebus.Messages;
using Rebus.Pipeline;
using Rebus.Threading;
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
    public class SimpleRetryStrategyStep : IRetryStrategyStep, IInitializable, IDisposable
    {
        const string BackgroundTaskName = "CleanupTrackedErrors";
        static readonly TimeSpan MoveToErrorQueueFailedPause = TimeSpan.FromSeconds(5);
        static ILog _log;

        static SimpleRetryStrategyStep()
        {
            RebusLoggerFactory.Changed += f => _log = f.GetCurrentClassLogger();
        }

        readonly ConcurrentDictionary<string, ErrorTracking> _trackedErrors = new ConcurrentDictionary<string, ErrorTracking>();
        readonly SimpleRetryStrategySettings _simpleRetryStrategySettings;
        readonly AsyncTask _cleanupOldTrackedErrorsTask;
        readonly ITransport _transport;

        bool _disposed;

        /// <summary>
        /// Constructs the step, using the given transport and settings
        /// </summary>
        public SimpleRetryStrategyStep(ITransport transport, SimpleRetryStrategySettings simpleRetryStrategySettings)
        {
            _transport = transport;
            _simpleRetryStrategySettings = simpleRetryStrategySettings;
            _cleanupOldTrackedErrorsTask = new AsyncTask(BackgroundTaskName, CleanupOldTrackedErrors)
            {
                Interval = TimeSpan.FromMinutes(1)
            };
        }

        /// <summary>
        /// Last-resort shudown of the background task for cleaning up tracked errors (<see cref="BackgroundTaskName"/>)
        /// </summary>
        ~SimpleRetryStrategyStep()
        {
            Dispose(false);
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
                        shortErrorDescription: "Received message with empty or absent 'rbs2-msg-id' header");

                return;
            }

            if (HasFailedTooManyTimes(messageId))
            {
                await MoveMessageToErrorQueue(messageId, transportMessage, transactionContext, GetErrorDescriptionFor(messageId), GetErrorDescriptionFor(messageId, brief: true));

                RemoveErrorTracking(messageId);

                return;
            }

            try
            {
                await next();
            }
            catch (Exception exception)
            {
                var errorTracking = _trackedErrors.AddOrUpdate(messageId,
                    id => new ErrorTracking(exception),
                    (id, tracking) => tracking.AddError(exception));

                _log.Warn("Unhandled exception {0} while handling message with ID {1}: {2}",
                    errorTracking.Errors.Count(), messageId, exception);

                transactionContext.Abort();
            }
        }

        void RemoveErrorTracking(string messageId)
        {
            ErrorTracking dummy;
            _trackedErrors.TryRemove(messageId, out dummy);
        }

        /// <summary>
        /// Initializes the step, starting the background task that cleans up old tracked errors
        /// </summary>
        public void Initialize()
        {
            _cleanupOldTrackedErrorsTask.Start();
        }

        async Task CleanupOldTrackedErrors()
        {
            ErrorTracking _;

            _trackedErrors
                .ToList()
                .Where(e => e.Value.ElapsedSinceLastError > TimeSpan.FromMinutes(10))
                .ForEach(tracking => _trackedErrors.TryRemove(tracking.Key, out _));
        }

        string GetErrorDescriptionFor(string messageId, bool brief = false)
        {
            ErrorTracking errorTracking;

            if (!_trackedErrors.TryGetValue(messageId, out errorTracking))
            {
                return "Could not get error details for the message";
            }

            if (brief)
            {
                return string.Format("{0} unhandled exceptions", errorTracking.Errors.Count());
            }

            var fullExceptionInfo = string.Join(Environment.NewLine, errorTracking.Errors.Select(e => string.Format("{0}: {1}", e.Time, e.Exception)));

            return string.Format("{0} unhandled exceptions: {1}", errorTracking.Errors.Count(), fullExceptionInfo);
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

        bool HasFailedTooManyTimes(string messageId)
        {
            ErrorTracking existingTracking;
            var hasTrackingForThisMessage = _trackedErrors.TryGetValue(messageId, out existingTracking);

            if (!hasTrackingForThisMessage) return false;

            var hasFailedTooManyTimes = existingTracking.ErrorCount >= _simpleRetryStrategySettings.MaxDeliveryAttempts;

            return hasFailedTooManyTimes;
        }

        class ErrorTracking
        {
            readonly ConcurrentQueue<CaughtException> _caughtExceptions = new ConcurrentQueue<CaughtException>();

            public ErrorTracking(Exception exception)
            {
                AddError(exception);
            }

            public int ErrorCount
            {
                get { return _caughtExceptions.Count; }
            }

            public IEnumerable<CaughtException> Errors
            {
                get { return _caughtExceptions; }
            }

            public ErrorTracking AddError(Exception caughtException)
            {
                _caughtExceptions.Enqueue(new CaughtException(caughtException));
                return this;
            }

            public TimeSpan ElapsedSinceLastError
            {
                get
                {
                    var timeOfMostRecentError = _caughtExceptions.Max(e => e.Time);

                    var elapsedSinceLastError = DateTime.UtcNow - timeOfMostRecentError;

                    return elapsedSinceLastError;
                }
            }
        }

        class CaughtException
        {
            public CaughtException(Exception exception)
            {
                Exception = exception;
                Time = DateTime.UtcNow;
            }

            public Exception Exception { get; set; }
            public DateTime Time { get; set; }
        }

        public void Dispose()
        {
            Dispose(true);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed) return;

            try
            {
                _cleanupOldTrackedErrorsTask.Dispose();
            }
            finally
            {
                _disposed = true;
            }
        }
    }
}