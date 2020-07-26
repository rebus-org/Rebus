using Rebus.Logging;
using Rebus.Messages;
using Rebus.Transport;
using System;
using System.Threading.Tasks;

namespace Rebus.Retry.CircuitBreaker
{
    internal class CircuitBreakerErrorHandler : IErrorHandler
    {
        private readonly ICircuitBreaker circuitBreaker;
        private readonly IErrorHandler innerErrorHandler;
        private ILog _log;

        public CircuitBreakerErrorHandler(ICircuitBreaker circuitBreaker, IErrorHandler innerErrorHandler
            , IRebusLoggerFactory rebusLoggerFactory)
        {
            this.circuitBreaker = circuitBreaker;
            this.innerErrorHandler = innerErrorHandler ?? throw new ArgumentNullException(nameof(innerErrorHandler));
            _log = rebusLoggerFactory?.GetLogger<CircuitBreakerErrorHandler>() ?? throw new ArgumentNullException(nameof(rebusLoggerFactory));
        }

        public async Task HandlePoisonMessage(TransportMessage transportMessage, ITransactionContext transactionContext, Exception exception)
        {
            // The `IErrorHandler` is first trickered after a handler has failed 5 times
            // This means, that if we have a faulty service throwing an exception, 
            // the service still getting taxed at least 5 times before we trip 1 attempt.
            // If we configure circuit breaker to have 5 attempts within an interval, then the service will get taxed 25 times!
            // Question is whether we should move this logic away from the IErrorHandler and into a IIncomingStep, to ensure that an external dependency
            // is taxed more than necessary.
            
            // TODO: What to do?

            circuitBreaker.Trip(exception);
                        
            await innerErrorHandler.HandlePoisonMessage(transportMessage, transactionContext, exception);
        }
    }
}
