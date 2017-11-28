using System;
using System.Linq;
using System.Threading.Tasks;
using Rebus.Exceptions;
using Rebus.Extensions;
using Rebus.Messages;
using Rebus.Pipeline;
using Rebus.Transport;

namespace Rebus.Retry.FailFast
{
    /// <summary>
    /// Incoming message pipeline step that implements a fail-fast mechanism - if the message has failed once
    /// with a specific exceptions, it get marked as "failed too many times". This allows the <seealso cref="Simple.SimpleRetryStrategyStep"/>
    /// to send the message to the error queue.
    /// </summary>
    [StepDocumentation(@"Checks if a message has failed with specific exceptions and marks it as ""failed too many times"". 
This allows the SimpleRetryStrategyStep to move it to the error queue.")]
    public class FailFastStep : IIncomingStep
    {
        readonly IErrorTracker _errorTracker;
        readonly IFailFastChecker _failFastChecker;

        /// <summary>
        /// Constructs the step, using the given error tracker
        /// </summary>
        public FailFastStep(IErrorTracker errorTracker, IFailFastChecker failFastChecker)
        {
            if (errorTracker == null) throw new ArgumentNullException(nameof(errorTracker));
            if (failFastChecker == null) throw new ArgumentNullException(nameof(failFastChecker));

            _errorTracker = errorTracker;
            _failFastChecker = failFastChecker;
        }

        /// <summary>
        /// Checks if there are any registered exceptions to the current message and
        /// if all of them are <see cref="FailFastException"/>, then mark the message
        /// as failed too many times.
        /// </summary>
        public async Task Process(IncomingStepContext context, Func<Task> next)
        {
            var transportMessage = context.Load<TransportMessage>();
            var transactionContext = context.Load<ITransactionContext>();
            var messageId = transportMessage.Headers.GetValueOrNull(Headers.MessageId);

            if (!string.IsNullOrWhiteSpace(messageId))
            {
                var trackedExceptions = _errorTracker.GetExceptions(messageId).ToArray();
                if (trackedExceptions.Any() && trackedExceptions.All(e => _failFastChecker.ShouldFailFast(messageId, e)))
                {
                    MarkAsFailedTooManyTimes(messageId, trackedExceptions.Last());
                }
            }

            await next();
        }

        void MarkAsFailedTooManyTimes(string messageId, Exception exception)
        {
            while (!_errorTracker.HasFailedTooManyTimes(messageId))
            {
                _errorTracker.RegisterError(messageId, exception);
            }
        }
    }
}