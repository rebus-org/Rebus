using System;
using Rebus.Config;
using Rebus.Retry.Info;
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
            var exceptionInfoFactory = c.Get<IExceptionInfoFactory>();
            return new InMemErrorTracker(
                retryStrategySettings,
                asyncTaskFactory,
                rebusTime,
                exceptionLogger,
                exceptionInfoFactory
            );
        });
    }

    /// <summary>
    /// Configures Rebus to use in-mem exception infos that provide the original exception via <see cref="ExceptionInfo.ConvertTo{TExceptionInfo}"/>
    /// </summary>
    public static void UseInMemExceptionInfos(this StandardConfigurer<IErrorTracker> configurer)
    {
        if (configurer == null) throw new ArgumentNullException(nameof(configurer));
        configurer.OtherService<IExceptionInfoFactory>().Register(c => new InMemExceptionInfoFactory());
    }
}