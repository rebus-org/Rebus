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
            if (simpleRetryStrategySettings == null) throw new ArgumentNullException(nameof(simpleRetryStrategySettings));
            if (errorTracker == null) throw new ArgumentNullException(nameof(errorTracker));
            if (errorHandler == null) throw new ArgumentNullException(nameof(errorHandler));

            _simpleRetryStrategySettings = simpleRetryStrategySettings;
            _errorTracker = errorTracker;
            _errorHandler = errorHandler;
        }

        /// <summary>
        /// Gets the retry step with appropriate settings for this <see cref="SimpleRetryStrategy"/>
        /// </summary>
        public IRetryStrategyStep GetRetryStep()
        {
            return new SimpleRetryStrategyStep(_simpleRetryStrategySettings, _errorTracker, _errorHandler);
        }
    }
}