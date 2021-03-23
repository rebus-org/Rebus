using System;
using System.Threading;
using Rebus.Config;
using Rebus.ExclusiveLocks;
using Rebus.Pipeline;

namespace Rebus.Sagas.Exclusive
{
    /// <summary>
    /// Configuration extensions for optional in-process locking of saga instances
    /// </summary>
    public static class ExclusiveAccessConfigurationExtensions
    {
        /// <summary>
        /// Forces exclusive access
        /// </summary>
        public static void EnforceExclusiveAccess(this StandardConfigurer<ISagaStorage> configurer, int maxLockBuckets = 1000)
        {
            if (configurer == null) throw new ArgumentNullException(nameof(configurer));

            configurer
                .OtherService<IPipeline>()
                .Decorate(c =>
                {
                    var pipeline = c.Get<IPipeline>();
                    var cancellationToken = c.Get<CancellationToken>();
                    var step = new SemaphoreSlimExclusiveSagaAccessIncomingStep(maxLockBuckets, cancellationToken);

                    return new PipelineStepInjector(pipeline)
                        .OnReceive(step, PipelineRelativePosition.Before, typeof(LoadSagaDataStep));
                });
        }

        /// <summary>
        /// Forces exclusive access using IExclusiveAccessLock
        /// </summary>
        public static void EnforceExclusiveAccessViaLocker(this StandardConfigurer<ISagaStorage> configurer, string lockPrefix = null, int maxLockBuckets = 1000)
        {
            if (configurer == null) throw new ArgumentNullException(nameof(configurer));

            configurer
                .OtherService<IPipeline>()
                .Decorate(c =>
                {
                    var pipeline = c.Get<IPipeline>();
                    var cancellationToken = c.Get<CancellationToken>();
                    var locker = c.Get<IExclusiveAccessLock>();
                    var step = new EnforceExclusiveSagaAccessIncomingStep(locker, maxLockBuckets, lockPrefix ?? "sagalock_", cancellationToken);

                    return new PipelineStepInjector(pipeline)
                        .OnReceive(step, PipelineRelativePosition.Before, typeof(LoadSagaDataStep));
                });
        }
    }
}