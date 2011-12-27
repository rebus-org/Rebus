namespace Rebus.Configuration.Configurers
{
    public class EndpointMappingsConfigurer
    {
        readonly IContainerAdapter containerAdapter;

        public EndpointMappingsConfigurer(IContainerAdapter containerAdapter)
        {
            this.containerAdapter = containerAdapter;
        }

        public void Use<T>(T instance) where T : IDetermineDestination
        {
            containerAdapter.RegisterInstance(instance, typeof(IDetermineDestination));
        }
    }
}