using System;

namespace Rebus.Configuration
{
    /// <summary>
    /// Configurer that allows for configuring which implementation of <see cref="IDetermineDestination"/> that should be used
    /// </summary>
    public class RebusRoutingConfigurer : BaseConfigurer
    {
        internal RebusRoutingConfigurer(ConfigurationBackbone backbone)
            : base(backbone)
        {
        }

        /// <summary>
        /// Uses the specified implementation of <see cref="IDetermineDestination"/> to determine who owns messages
        /// </summary>
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
            Use(new DetermineDestinationFromRebusConfigurationSection());
        }

        /// <summary>
        /// Configures Rebus to expect endpoint mappings to be on Rebus form.
        /// </summary>
        public void FromRebusConfigurationSectionWithFilter(Func<Type, bool> typeFilter)
        {
            Use(new DetermineDestinationFromRebusConfigurationSection(typeFilter));
        }
    }
}