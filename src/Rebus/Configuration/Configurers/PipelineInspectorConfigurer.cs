namespace Rebus.Configuration.Configurers
{
    public class PipelineInspectorConfigurer
    {
        IContainerAdapter containerAdapter;

        public PipelineInspectorConfigurer(IContainerAdapter containerAdapter)
        {
            this.containerAdapter = containerAdapter;
        }

        public void Use<T>(T instance) where T : IInspectHandlerPipeline
        {
            containerAdapter.RegisterInstance(instance, typeof(IInspectHandlerPipeline));
        }
    }
}