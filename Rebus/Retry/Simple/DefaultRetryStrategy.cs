using System;
using System.Threading;
using Rebus.Logging;
using Rebus.Retry.FailFast;

namespace Rebus.Retry.Simple;

/// <summary>
/// Implementation of <see cref="IRetryStrategy"/> that tracks errors in memory
/// </summary>
public class DefaultRetryStrategy : IRetryStrategy
{
    readonly RetryStrategySettings _retryStrategySettings;
    readonly IRebusLoggerFactory _rebusLoggerFactory;
    readonly IErrorTracker _errorTracker;
    readonly IErrorHandler _errorHandler;
    readonly IFailFastChecker _failFastChecker;
    readonly CancellationToken _cancellationToken;

    /// <summary>
    /// Constructs the retry strategy with the given settings, creating an error queue with the configured name if necessary
    /// </summary>
    public DefaultRetryStrategy(RetryStrategySettings retryStrategySettings, IRebusLoggerFactory rebusLoggerFactory, IErrorTracker errorTracker, IErrorHandler errorHandler, IFailFastChecker failFastChecker, CancellationToken cancellationToken)
    {
        _retryStrategySettings = retryStrategySettings ?? throw new ArgumentNullException(nameof(retryStrategySettings));
        _rebusLoggerFactory = rebusLoggerFactory ?? throw new ArgumentNullException(nameof(rebusLoggerFactory));
        _errorTracker = errorTracker ?? throw new ArgumentNullException(nameof(errorTracker));
        _errorHandler = errorHandler ?? throw new ArgumentNullException(nameof(errorHandler));
        _failFastChecker = failFastChecker ?? throw new ArgumentNullException(nameof(failFastChecker));
        _cancellationToken = cancellationToken;
    }

    /// <summary>
    /// Gets the retry step with appropriate settings for this <see cref="DefaultRetryStrategy"/>
    /// </summary>
    public IRetryStep GetRetryStep() => new DefaultRetryStep(
        _rebusLoggerFactory,
        _errorHandler,
        _errorTracker,
        _failFastChecker,
        _retryStrategySettings.SecondLevelRetriesEnabled,
        _cancellationToken
    );
}