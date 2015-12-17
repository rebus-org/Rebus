using NLog;
using Rebus.Config;

namespace Rebus.NLog
{
    /// <summary>
    /// Configuration extensions for setting up logging with NLog
    /// </summary>
    public static class NLogConfigurationExtensions
    {
        /// <summary>
        /// Configures Rebus to use NLog for all of its internal logging, getting its loggers by calling logger <see cref="LogManager.GetLogger(string)"/>.
        /// After this method is called, a custom layout renderer will be available under the <see cref="RebusCorrelationIdLayoutRenderer.ItemName"/> variable,
        /// allowing your output pattern to incude the correlation ID of the message currently being handled by including <code>${rebus-correlation-id}</code>
        /// in the format string. You may register the layout renderer manually if you like by calling <see cref="RebusCorrelationIdLayoutRenderer.Register()"/>
        /// </summary>
        public static void NLog(this RebusLoggingConfigurer configurer)
        {
            configurer.Use(new NLogLoggerFactory());

            RebusCorrelationIdLayoutRenderer.Register();
        }
    }
}