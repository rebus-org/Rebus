namespace Rebus.Configuration.Configurers
{
    public class DiscoveryConfigurer
    {
        readonly HandlerLoader handlerLoader;

        public DiscoveryConfigurer(IContainerAdapter containerAdapter)
        {
            handlerLoader = new HandlerLoader(containerAdapter);
        }

        public HandlerLoader Handlers
        {
            get { return handlerLoader; }
        }
    }
}