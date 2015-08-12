using System;

namespace Rebus.Configuration
{
    /// <summary>
    /// Extended root configurer that allows for configuring how all internal Rebus components do their logging
    /// </summary>
    public class RebusConfigurerWithLogging : RebusConfigurer
    {
        internal RebusConfigurerWithLogging(ConfigurationBackbone backbone) : base(backbone)
        {
        }

        /// <summary>
        /// Invokes the configurer that allows for configuring how Rebus does its logging
        /// </summary>
        public RebusConfigurer Logging(Action<LoggingConfigurer> configurer)
        {
            configurer(new LoggingConfigurer(Backbone));
            return this;
        }
    }
}