namespace Rebus.Configuration
{
    public class PipelineInspectorConfigurer : BaseConfigurer
    {
        public PipelineInspectorConfigurer(ConfigurationBackbone backbone)
            : base(backbone)
        {
        }

        public void Use(IInspectHandlerPipeline inspector)
        {
            Backbone.InspectHandlerPipeline = inspector;
        }
    }
}