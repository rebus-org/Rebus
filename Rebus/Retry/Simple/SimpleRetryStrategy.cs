using System;

namespace Rebus.Retry.Simple
{
    /// <summary>
    /// Implementation of <see cref="IRetryStrategy"/> that tracks errors in memory
    /// </summary>
    public class SimpleRetryStrategy : IRetryStrategy
    {
        readonly SimpleRetryStrategySettings _simpleRetryStrategySettings;
        readonly IErrorTracker _errorTracker;
        readonly IErrorHandler _errorHandler;

        /// <summary>
        /// Constructs the retry strategy with the given settings, creating an error queue with the configured name if necessary
        /// </summary>
        public SimpleRetryStrategy(SimpleRetryStrategySettings simpleRetryStrategySettings, IErrorTracker errorTracker, IErrorHandler errorHandler)
        {
            _simpleRetryStrategySettings = simpleRetryStrategySettings ?? throw new ArgumentNullException(nameof(simpleRetryStrategySettings));
            _errorTracker = errorTracker ?? throw new ArgumentNullException(nameof(errorTracker));
            _errorHandler = errorHandler ?? throw new ArgumentNullException(nameof(errorHandler));
        }

        /// <summary>
        /// Gets the retry step with appropriate settings for this <see cref="SimpleRetryStrategy"/>
        /// </summary>
        public IRetryStrategyStep GetRetryStep() => new SimpleRetryStrategyStep(
            _simpleRetryStrategySettings,
            _errorTracker,
            _errorHandler
        );
    }
}