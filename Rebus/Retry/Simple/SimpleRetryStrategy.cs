using Rebus.Pipeline;
using Rebus.Transport;

namespace Rebus.Retry.Simple
{
    /// <summary>
    /// Implementation of <see cref="IRetryStrategy"/> that tracks errors in memory
    /// </summary>
    public class SimpleRetryStrategy : IRetryStrategy
    {
        readonly ITransport _transport;
        readonly SimpleRetryStrategySettings _simpleRetryStrategySettings;

        public SimpleRetryStrategy(ITransport transport, SimpleRetryStrategySettings simpleRetryStrategySettings)
        {
            _transport = transport;
            _simpleRetryStrategySettings = simpleRetryStrategySettings;

            var errorQueueAddress = _simpleRetryStrategySettings.ErrorQueueAddress;
            
            _transport.CreateQueue(errorQueueAddress);
        }

        public IIncomingStep GetRetryStep()
        {
            return new SimpleRetryStrategyStep(_transport, _simpleRetryStrategySettings);
        }
    }
}