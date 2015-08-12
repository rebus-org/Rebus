namespace Rebus.Configuration
{
    /// <summary>
    /// Configurer to configure which kind of pipeline inspector should be installed.
    /// </summary>
    public class PipelineInspectorConfigurer : BaseConfigurer
    {
        internal PipelineInspectorConfigurer(ConfigurationBackbone backbone)
            : base(backbone)
        {
        }

        /// <summary>
        /// Uses the specified implementation of <see cref="IInspectHandlerPipeline"/>
        /// </summary>
        public void Use(IInspectHandlerPipeline inspector)
        {
            Backbone.InspectHandlerPipeline = inspector;
        }
    }
}