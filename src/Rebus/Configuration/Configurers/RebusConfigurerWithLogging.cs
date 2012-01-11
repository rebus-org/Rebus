using System;

namespace Rebus.Configuration.Configurers
{
    public class RebusConfigurerWithLogging : RebusConfigurer
    {
        public RebusConfigurerWithLogging(IContainerAdapter containerAdapter) : base(containerAdapter)
        {
        }

        public RebusConfigurer Logging(Action<LoggingConfigurer> configureLogging)
        {
            configureLogging(new LoggingConfigurer());
            return new RebusConfigurer(containerAdapter);
        }
    }
}