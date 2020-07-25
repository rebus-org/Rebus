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
            circuitBreaker.Trip(exception);
                        
            await innerErrorHandler.HandlePoisonMessage(transportMessage, transactionContext, exception);
        }
    }
}
