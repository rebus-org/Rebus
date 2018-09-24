using System;
using System.Threading.Tasks;
using Rebus.Bus;
using Rebus.Exceptions;
using Rebus.Messages;
using Rebus.Pipeline;
// ReSharper disable ArgumentsStyleLiteral

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
            _errorTracker = errorTracker ?? throw new ArgumentNullException(nameof(errorTracker));
            _failFastChecker = failFastChecker ?? throw new ArgumentNullException(nameof(failFastChecker));
        }

        /// <summary>
        /// Checks if there are any registered exceptions to the current message and
        /// if all of them are <see cref="FailFastException"/>, then mark the message
        /// as failed too many times.
        /// </summary>
        public async Task Process(IncomingStepContext context, Func<Task> next)
        {
            try
            {
                await next();
            }
            catch (Exception exception)
            {
                var transportMessage = context.Load<TransportMessage>();
                var messageId = transportMessage.GetMessageId();
                if (_failFastChecker.ShouldFailFast(messageId, exception))
                {
                    _errorTracker.MarkAsFinal(messageId);
                }
                throw;
            }
        }
    }
}