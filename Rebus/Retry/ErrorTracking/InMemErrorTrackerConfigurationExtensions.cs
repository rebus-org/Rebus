using System;
using Rebus.Config;
using Rebus.Retry.Simple;
using Rebus.Threading;
using Rebus.Time;

namespace Rebus.Retry.ErrorTracking;

/// <summary>
/// Configuration extension for the in-mem error tracker
/// </summary>
public static class InMemErrorTrackerConfigurationExtensions
{
    /// <summary>
    /// Configures Rebus to track delivery attempts in memory. This is the default error tracker, so there's no need to explicitly call this method.
    /// </summary>
    public static void UseInMemErrorTracker(this StandardConfigurer<IErrorTracker> configurer)
    {
        if (configurer == null) throw new ArgumentNullException(nameof(configurer));
        configurer.Register(c =>
        {
            var retryStrategySettings = c.Get<RetryStrategySettings>();
            var asyncTaskFactory = c.Get<IAsyncTaskFactory>();
            var rebusTime = c.Get<IRebusTime>();
            var exceptionLogger = c.Get<IExceptionLogger>();
            return new InMemErrorTracker(
                retryStrategySettings,
                asyncTaskFactory,
                rebusTime,
                exceptionLogger
            );
        });
    }
}