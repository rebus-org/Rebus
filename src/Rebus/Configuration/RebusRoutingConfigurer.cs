namespace Rebus.Configuration
{
    public class RebusRoutingConfigurer : BaseConfigurer
    {
        public RebusRoutingConfigurer(ConfigurationBackbone backbone)
            : base(backbone)
        {
        }

        public void Use(IDetermineDestination determineDestination)
        {
            Backbone.DetermineDestination = determineDestination;
        }

        /// <summary>
        /// Configures Rebus to pick up endpoint mappings in NServiceBus format from the current app.config/web.config.
        /// </summary>
        public void FromNServiceBusConfiguration()
        {
            Use(new DetermineDestinationFromNServiceBusEndpointMappings(new StandardAppConfigLoader()));
        }

        /// <summary>
        /// Configures Rebus to expect endpoint mappings to be on Rebus form.
        /// </summary>
        public void FromRebusConfigurationSection()
        {
            Use(new DetermineDestinationFromConfigurationSection());
        }
    }
}