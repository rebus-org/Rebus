using System;
using System.Threading;
using Rebus.Config;
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
        public static void EnforceExclusiveAccess(this StandardConfigurer<ISagaStorage> configurer)
        {
            if (configurer == null) throw new ArgumentNullException(nameof(configurer));

            configurer
                .OtherService<IPipeline>()
                .Decorate(c =>
                {
                    var pipeline = c.Get<IPipeline>();
                    var cancellationToken = c.Get<CancellationToken>();
                    var step = new NewEnforceExclusiveSagaAccessIncomingStep(1000, cancellationToken);

                    return new PipelineStepInjector(pipeline)
                        .OnReceive(step, PipelineRelativePosition.Before, typeof(LoadSagaDataStep));
                });
        }

        /// <summary>
        /// Forces exclusive using a lockhandler defined by <see cref="IExclusiveSagaAccessLock"/>
        /// </summary>
        public static void EnforceExclusiveAccess(this StandardConfigurer<ISagaStorage> configurer, IExclusiveSagaAccessLock locker)
        {
            if (configurer == null) throw new ArgumentNullException(nameof(configurer));

            configurer
                .OtherService<IPipeline>()
                .Decorate(c =>
                {
                    var pipeline = c.Get<IPipeline>();
                    var cancellationToken = c.Get<CancellationToken>();
                    var step = new EnforceExclusiveSagaAccessIncomingStep(locker, cancellationToken);

                    return new PipelineStepInjector(pipeline)
                        .OnReceive(step, PipelineRelativePosition.Before, typeof(LoadSagaDataStep));
                });
        }
    }
}