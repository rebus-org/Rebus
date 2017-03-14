using Rebus.Config;

namespace Rebus.Pipeline
{
    /// <summary>
    /// Configuration extensions for the experimental expression-based pipeline invoker
    /// </summary>
    public static class CompiledPipelineInvokerConfigurationExtensions
    {
        /// <summary>
        /// Configures Rebus to use a compiled expression-based invocation function for both receive and send pipelines
        /// </summary>
        public static void UseExperimentalPipelineInvoker(this OptionsConfigurer configurer)
        {
            configurer.Register<IPipelineInvoker>(c =>
            {
                var pipeline = c.Get<IPipeline>();

                return new CompiledPipelineInvoker(pipeline);
            });
        }
    }
}