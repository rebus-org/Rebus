using System;
using log4net;
using Rebus.Config;

namespace Rebus.Log4net
{
    /// <summary>
    /// Configuration extensions for setting up logging with Log4net
    /// </summary>
    public static class Log4NetConfigurationExtensions
    {
        /// <summary>
        /// Configures Rebus to use Log4Net for all of its internal logging, getting its loggers by calling logger <see cref="LogManager.GetLogger(Type)"/>
        /// </summary>
        public static void Log4Net(this RebusLoggingConfigurer configurer)
        {
            configurer.Use(new Log4NetLoggerFactory());
        }
    }
}