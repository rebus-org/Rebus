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

        /// <summary>
        /// Constructs the retry strategy with the given settings, creating an error queue with the configured name if necessary
        /// </summary>
        public SimpleRetryStrategy(ITransport transport, SimpleRetryStrategySettings simpleRetryStrategySettings)
        {
            _transport = transport;
            _simpleRetryStrategySettings = simpleRetryStrategySettings;

            var errorQueueAddress = _simpleRetryStrategySettings.ErrorQueueAddress;
            
            _transport.CreateQueue(errorQueueAddress);
        }

        /// <summary>
        /// Gets the retry step with appropriate settings for this <see cref="SimpleRetryStrategy"/>
        /// </summary>
        public IRetryStrategyStep GetRetryStep()
        {
            return new SimpleRetryStrategyStep(_transport, _simpleRetryStrategySettings);
        }
    }
}