using System;

namespace Rebus.Configuration
{
    /// <summary>
    /// Configurer that allows for configuring which implementation of <see cref="IDetermineMessageOwnership"/> that should be used
    /// </summary>
    public class RebusRoutingConfigurer : BaseConfigurer
    {
        internal RebusRoutingConfigurer(ConfigurationBackbone backbone)
            : base(backbone)
        {
        }

        /// <summary>
        /// Uses the specified implementation of <see cref="IDetermineMessageOwnership"/> to determine who owns messages
        /// </summary>
        public void Use(IDetermineMessageOwnership determineMessageOwnership)
        {
            Backbone.DetermineMessageOwnership = determineMessageOwnership;
        }

        /// <summary>
        /// Configures Rebus to pick up endpoint mappings in NServiceBus format from the current app.config/web.config.
        /// </summary>
        public void FromNServiceBusConfiguration()
        {
            Use(new DetermineMessageOwnershipFromNServiceBusEndpointMappings(new StandardAppConfigLoader()));
        }

        /// <summary>
        /// Configures Rebus to expect endpoint mappings to be on Rebus form.
        /// </summary>
        public void FromRebusConfigurationSection()
        {
            Use(new DetermineMessageOwnershipFromRebusConfigurationSection());
        }

        /// <summary>
        /// Configures Rebus to expect endpoint mappings to be on Rebus form.
        /// </summary>
        public void FromRebusConfigurationSectionWithFilter(Func<Type, bool> typeFilter)
        {
            Use(new DetermineMessageOwnershipFromRebusConfigurationSection(typeFilter));
        }
    }
}