namespace Rebus.Configuration
{
    public static class EndpointMappingsExtensions
    {
        /// <summary>
        /// Configures Rebus to pick up endpoint mappings in NServiceBus format from the current app.config/web.config.
        /// </summary>
        public static void FromNServiceBusConfiguration(this RebusRoutingConfigurer configurer)
        {
            configurer.Use(new DetermineDestinationFromNServiceBusEndpointMappings(new StandardAppConfigLoader()));
        }

        /// <summary>
        /// Configures Rebus to expect endpoint mappings to be on Rebus form.
        /// </summary>
        public static void FromRebusConfigurationSection(this RebusRoutingConfigurer configurer)
        {
            configurer.Use(new DetermineDestinationFromConfigurationSection());
        }
    }
}