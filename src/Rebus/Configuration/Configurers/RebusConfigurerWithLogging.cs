using System;

namespace Rebus.Configuration.Configurers
{
    /// <summary>
    /// Special <see cref="RebusConfigurer"/> that allows for logging to be configured. Returning this one
    /// only once ensures that logging will be configured as the first thing during the configuration
    /// spell, allowing other configurers to log stuff and have their output logged to the right place.
    /// </summary>
    public class RebusConfigurerWithLogging : RebusConfigurer
    {
        public RebusConfigurerWithLogging(IContainerAdapter containerAdapter) : base(containerAdapter)
        {
            containerAdapter.RegisterInstance(containerAdapter, typeof(IActivateHandlers));
        }

        public RebusConfigurer Logging(Action<LoggingConfigurer> configureLogging)
        {
            configureLogging(new LoggingConfigurer());
            return this;
        }
    }
}