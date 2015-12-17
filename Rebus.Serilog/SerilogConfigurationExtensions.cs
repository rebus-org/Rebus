using Rebus.Config;
using Serilog;
using Serilog.Configuration;

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
            configurer.Use(new SerilogLoggerFactory(loggerConfiguration));
        }

        /// <summary>
        /// Configures Rebus to use Serilog for all of its internal logging, deriving its loggers from the given <see cref="ILogger"/> base logger
        /// </summary>
        public static void Serilog(this RebusLoggingConfigurer configurer, ILogger baseLogger)
        {
            configurer.Use(new SerilogLoggerFactory(baseLogger));
        }

        /// <summary>
        /// Configures Serilog to add the correlation ID of the Rebus message currently being handled to log events as the <paramref name="propertyName"/>
        /// field. Does nothing when called outside of a message handler.
        /// </summary>
        public static LoggerConfiguration WithRebusCorrelationId(this LoggerEnrichmentConfiguration configuration, string propertyName)
        {
            return configuration.With(new RebusCorrelationIdEnricher(propertyName));
        }
    }
}