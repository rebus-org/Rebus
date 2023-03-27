using System;
using System.Threading.Tasks;
using Rebus.Bus;
using Rebus.Config;
using Rebus.Logging;
using Rebus.Messages;
using Rebus.Pipeline;
using Rebus.Retry.Simple;
using Rebus.Transport;

namespace Rebus.Routing.Exceptions;

/// <summary>
/// Configuration extensions for configuring automatic forwarding on certain exception types
/// </summary>
public static class AutoForwardOnExceptionConfigurationExtensions
{
    /// <summary>
    /// Short-circuits the usual retry strategy by immediately forwarding the transport message to the specified queue when an
    /// exception of the type specified by <typeparamref name="TException"/> is caught. Please note that any outgoing message
    /// that have already been sent WILL BE SENT because the queue transaction is not rolled back.
    /// Use <paramref name="logLevel"/> to specify which log level to use when logging the forwarding action.
    /// </summary>
    public static StandardConfigurer<IRouter> ForwardOnException<TException>(this StandardConfigurer<IRouter> configurer, string destinationQueue, LogLevel logLevel, Func<TException, bool> shouldForward = null)
        where TException : Exception
    {
        configurer
            .OtherService<IPipeline>()
            .Decorate(c =>
            {
                var pipeline = c.Get<IPipeline>();
                var transport = c.Get<ITransport>();
                var rebusLoggerFactory = c.Get<IRebusLoggerFactory>();

                var shouldForwardException = shouldForward != null
                    ? (Func<Exception, bool>)(exception => shouldForward((TException)exception))
                    : (exception => true);

                var step = new ForwardOnExceptionsStep(typeof(TException), destinationQueue, transport, rebusLoggerFactory, logLevel, shouldForwardException);

                return new PipelineStepInjector(pipeline)
                    .OnReceive(step, PipelineRelativePosition.After, typeof(DefaultRetryStep));
            });

        return configurer;
    }

    [StepDocumentation("Wraps the invocation of the rest of the pipeline in a try-catch block, immediately forwarding the message to another queue when some specific exception (or any derived exception type) is caught.")]
    class ForwardOnExceptionsStep : IIncomingStep
    {
        readonly Type _exceptionType;
        readonly string _destinationQueue;
        readonly ITransport _transport;
        readonly LogLevel _logLevel;
        readonly Func<Exception, bool> _shouldForward;
        readonly ILog _logger;

        public ForwardOnExceptionsStep(Type exceptionType, string destinationQueue, ITransport transport, IRebusLoggerFactory rebusLoggerFactory, LogLevel logLevel, Func<Exception, bool> shouldForward)
        {
            _logger = rebusLoggerFactory.GetLogger<ForwardOnExceptionsStep>();
            _exceptionType = exceptionType;
            _destinationQueue = destinationQueue;
            _transport = transport;
            _logLevel = logLevel;
            _shouldForward = shouldForward;
        }

        public async Task Process(IncomingStepContext context, Func<Task> next)
        {
            var errorDetails = "";
            var caughtException = false;

            try
            {
                await next();
            }
            catch (Exception exception)
            {
                if (!_exceptionType.IsInstanceOfType(exception))
                {
                    throw;
                }

                if (!_shouldForward(exception))
                {
                    throw;
                }

                caughtException = true;
                errorDetails = $"Caught exception: {exception}";
            }

            if (!caughtException) return;

            var transactionContext = context.Load<ITransactionContext>();
            var transportMessage = context.Load<TransportMessage>();

            var clone = transportMessage.Clone();
            clone.Headers[Headers.ErrorDetails] = errorDetails;

            const string message = "Forwarding message {messageLabel} to queue '{queueName}' because of: {exception}";
            var messageLabel = clone.GetMessageLabel();

            switch (_logLevel)
            {
                case LogLevel.Debug:
                    _logger.Debug(message, messageLabel, _destinationQueue, errorDetails);
                    break;
                case LogLevel.Info:
                    _logger.Info(message, messageLabel, _destinationQueue, errorDetails);
                    break;
                case LogLevel.Warn:
                    _logger.Warn(message, messageLabel, _destinationQueue, errorDetails);
                    break;
                case LogLevel.Error:
                    _logger.Error(message, messageLabel, _destinationQueue, errorDetails);
                    break;
                default:
                    throw new ArgumentOutOfRangeException($"Unknown log level: {_logLevel}");
            }

            await _transport.Send(_destinationQueue, clone, transactionContext);
        }
    }
}