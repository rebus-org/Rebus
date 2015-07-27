using Rebus.Config;
using Rebus.Logging;
using Serilog;

namespace Rebus.Serilog
{
    /// <summary>
    /// Configuration extensions for setting up logging with Serilog
    /// </summary>
    public static class SerilogConfigurationExtensions
    {
        /// <summary>
        /// Configures Rebus to use Serilog for all of its internal logging, deriving its logger by pulling a base logger from the given <see cref="LoggerConfiguration"/>
        /// </summary>
        public static void Serilog(this RebusLoggingConfigurer configurer, LoggerConfiguration loggerConfiguration)
        {
            RebusLoggerFactory.Current = new SerilogLoggerFactory(loggerConfiguration);
        }

        /// <summary>
        /// Configures Rebus to use Serilog for all of its internal logging, deriving its loggers from the given <see cref="ILogger"/> base logger
        /// </summary>
        public static void Serilog(this RebusLoggingConfigurer configurer, ILogger baseLogger)
        {
            RebusLoggerFactory.Current = new SerilogLoggerFactory(baseLogger);
        }
    }
}