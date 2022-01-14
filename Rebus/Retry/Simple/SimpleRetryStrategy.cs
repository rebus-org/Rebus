using System;
using System.Threading;
using Rebus.Logging;

namespace Rebus.Retry.Simple;

/// <summary>
/// Implementation of <see cref="IRetryStrategy"/> that tracks errors in memory
/// </summary>
public class SimpleRetryStrategy : IRetryStrategy
{
    readonly SimpleRetryStrategySettings _simpleRetryStrategySettings;
    readonly IRebusLoggerFactory _rebusLoggerFactory;
    readonly IErrorTracker _errorTracker;
    readonly IErrorHandler _errorHandler;
    readonly CancellationToken _cancellationToken;

    /// <summary>
    /// Constructs the retry strategy with the given settings, creating an error queue with the configured name if necessary
    /// </summary>
    public SimpleRetryStrategy(SimpleRetryStrategySettings simpleRetryStrategySettings, IRebusLoggerFactory rebusLoggerFactory, IErrorTracker errorTracker, IErrorHandler errorHandler, CancellationToken cancellationToken)
    {
        _simpleRetryStrategySettings = simpleRetryStrategySettings ?? throw new ArgumentNullException(nameof(simpleRetryStrategySettings));
        _rebusLoggerFactory = rebusLoggerFactory ?? throw new ArgumentNullException(nameof(rebusLoggerFactory));
        _errorTracker = errorTracker ?? throw new ArgumentNullException(nameof(errorTracker));
        _errorHandler = errorHandler ?? throw new ArgumentNullException(nameof(errorHandler));
        _cancellationToken = cancellationToken;
    }

    /// <summary>
    /// Gets the retry step with appropriate settings for this <see cref="SimpleRetryStrategy"/>
    /// </summary>
    public IRetryStrategyStep GetRetryStep() => new SimpleRetryStrategyStep(
        _simpleRetryStrategySettings,
        _rebusLoggerFactory,
        _errorTracker,
        _errorHandler,
        _cancellationToken
    );
}