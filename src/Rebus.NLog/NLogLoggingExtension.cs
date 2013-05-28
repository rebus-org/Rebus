using Rebus.Configuration;

namespace Rebus.NLog
{
    /// <summary>
    /// Extensions for configuring Rebus to use NLog for logging
    /// </summary>
    public static class NLogLoggingExtension
    {
        /// <summary>
        /// Configures Rebus to do its internal logging via NLog
        /// </summary>
        public static void NLog(this LoggingConfigurer configurer)
        {
            configurer.Use(new NLogLoggerFactory());
        }
    }
}
