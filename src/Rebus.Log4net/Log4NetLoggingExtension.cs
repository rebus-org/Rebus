using Rebus.Configuration;

namespace Rebus.Log4Net
{
    /// <summary>
    /// Extensions to <see cref="LoggingConfigurer"/> that allows for choosing Log4net for all of Rebus' internal logging needs
    /// </summary>
    public static class Log4NetLoggingExtension
    {
        /// <summary>
        /// Configures Rebus to use Log4net for all of its internal logging
        /// </summary>
        public static void Log4Net(this LoggingConfigurer configurer)
        {
            configurer.Use(new Log4NetLoggerFactory());
        }
    }
}