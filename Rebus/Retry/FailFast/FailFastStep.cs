using System;
using System.Linq;
using System.Threading.Tasks;
using Rebus.Bus;
using Rebus.Exceptions;
using Rebus.Extensions;
using Rebus.Messages;
using Rebus.Pipeline;
using Rebus.Transport;
// ReSharper disable SuggestBaseTypeForParameter

// ReSharper disable ArgumentsStyleLiteral

namespace Rebus.Retry.FailFast
{
    /// <summary>
    /// </summary>
    public class FailFastStep : IIncomingStep
    {
        readonly IErrorTracker _errorTracker;
        readonly IFailFastChecker _failFastChecker;
        readonly IErrorHandler _errorHandler;

        /// <summary>
        /// Incoming message pipeline step that implements a fail-fast mechanism - if the message has failed once
        /// with a specific exceptions, it get marked as "failed too many times". This allows the <seealso cref="Simple.SimpleRetryStrategyStep"/>
        /// to send the message to the error queue.
        /// </summary>
        public FailFastStep(IErrorTracker errorTracker, IFailFastChecker failFastChecker, IErrorHandler errorHandler)
        {
            _errorTracker = errorTracker ?? throw new ArgumentNullException(nameof(errorTracker));
            _failFastChecker = failFastChecker ?? throw new ArgumentNullException(nameof(failFastChecker));
            _errorHandler = errorHandler ?? throw new ArgumentNullException(nameof(errorHandler));
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

                var deadletterCommand = context.Load<ManualDeadletterCommand>();

                if (deadletterCommand != null)
                {
                    await ProcessDeadletterCommand(context, deadletterCommand);
                }
            }
            catch (Exception exception)
            {
                var transportMessage = context.Load<TransportMessage>();
                var messageId = transportMessage.GetMessageId();
                if (_failFastChecker.ShouldFailFast(messageId, exception))
                {
                    _errorTracker.RegisterError(messageId, exception, final: true);
                }

                throw;
            }
        }

        async Task ProcessDeadletterCommand(IncomingStepContext context, ManualDeadletterCommand deadletterCommand)
        {
            var originalTransportMessage = context.Load<OriginalTransportMessage>() ??
                                           throw new RebusApplicationException(
                                               "Could not find the original transport message in the current incoming step context");

            var transportMessage = originalTransportMessage.TransportMessage.Clone();
            var transactionContext = context.Load<ITransactionContext>();
            var exception = deadletterCommand.Exception;

            await _errorHandler.HandlePoisonMessage(transportMessage, transactionContext, exception);
        }
    }
}