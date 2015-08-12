using NLog;
using Rebus.Config;
using Rebus.Logging;

namespace Rebus.NLog
{
    /// <summary>
    /// Configuration extensions for setting up logging with NLog
    /// </summary>
    public static class NLogConfigurationExtensions
    {
        /// <summary>
        /// Configures Rebus to use NLog for all of its internal logging, getting its loggers by calling logger <see cref="LogManager.GetLogger(string)"/>
        /// </summary>
        public static void NLog(this RebusLoggingConfigurer configurer)
        {
            RebusLoggerFactory.Current = new NLogLoggerFactory();
        }
    }
}