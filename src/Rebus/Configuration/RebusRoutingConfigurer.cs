namespace Rebus.Configuration
{
    public class RebusRoutingConfigurer
    {
        readonly ConfigurationBackbone backbone;

        public RebusRoutingConfigurer(ConfigurationBackbone backbone)
        {
            this.backbone = backbone;
        }

        public void Use(IDetermineDestination determineDestination)
        {
            backbone.DetermineDestination = determineDestination;
        }

        public void FromNServiceBusConfiguration()
        {
            Use(new DetermineDestinationFromNServiceBusEndpointMappings(new StandardAppConfigLoader()));
        }
    }
}