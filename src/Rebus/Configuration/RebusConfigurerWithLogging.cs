using System;

namespace Rebus.Configuration
{
    public class RebusConfigurerWithLogging : RebusConfigurer
    {
        public RebusConfigurerWithLogging(ConfigurationBackbone backbone) : base(backbone)
        {
        }

        public RebusConfigurer Logging(Action<LoggingConfigurer> configurer)
        {
            configurer(new LoggingConfigurer(backbone));
            return this;
        }
    }
}