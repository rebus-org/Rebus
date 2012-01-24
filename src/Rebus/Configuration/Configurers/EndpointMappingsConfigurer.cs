namespace Rebus.Configuration.Configurers
{
    /// <summary>
    /// Configurer that allows for providing in instance of <see cref="IDetermineDestination"/>.
    /// </summary>
    public class EndpointMappingsConfigurer
    {
        readonly IContainerAdapter containerAdapter;

        /// <summary>
        /// Constructs the configurer with the specified container adapter.
        /// </summary>
        public EndpointMappingsConfigurer(IContainerAdapter containerAdapter)
        {
            this.containerAdapter = containerAdapter;
        }

        /// <summary>
        /// Makes the configurer insert the provided <see cref="IDetermineDestination"/> instance
        /// into the container adapter.
        /// </summary>
        public void Use<T>(T instance) where T : IDetermineDestination
        {
            containerAdapter.RegisterInstance(instance, typeof(IDetermineDestination));
        }
    }
}