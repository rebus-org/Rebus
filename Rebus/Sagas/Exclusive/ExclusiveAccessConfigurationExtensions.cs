using System;
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
                    var step = new EnforceExclusiveSagaAccessIncomingStep();

                    return new PipelineStepInjector(pipeline)
                        .OnReceive(step, PipelineRelativePosition.Before, typeof(LoadSagaDataStep));
                });
        }
    }
}