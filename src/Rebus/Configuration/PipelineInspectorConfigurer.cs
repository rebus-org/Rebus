namespace Rebus.Configuration
{
    public class PipelineInspectorConfigurer
    {
        readonly ConfigurationBackbone backbone;

        public PipelineInspectorConfigurer(ConfigurationBackbone backbone)
        {
            this.backbone = backbone;
        }

        public void Use(IInspectHandlerPipeline inspector)
        {
            backbone.InspectHandlerPipeline = inspector;
        }
    }
}