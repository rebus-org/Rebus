using NLog;
using Rebus.Configuration;
using Rebus.Shared;

namespace Rebus.NLog
{
    /// <summary>
    /// Extensions for configuring Rebus to use NLog for logging
    /// </summary>
    public static class NLogLoggingExtension
    {
        /// <summary>
        /// Default NLog thread context key to use for setting the correlation ID of the message currently being handled.
        /// </summary>
        public const string DefaultCorrelationIdPropertyKey = "CorrelationId";

        /// <summary>
        /// Configures Rebus to do its internal logging via NLog. Will automatically add a correlation ID variable to the NLog
        /// thread context (<see cref="MappedDiagnosticsContext"/>) under the key 'CorrelationId' (as specified by <see cref="DefaultCorrelationIdPropertyKey"/>)
        /// when handling messages, allowing log output to include that.
        /// </summary>
        public static void NLog(this LoggingConfigurer configurer)
        {
            configurer.Use(new NLogLoggerFactory());

            SetUpEventHandler(configurer, DefaultCorrelationIdPropertyKey);
        }

        /// <summary>
        /// Configures Rebus to use NLog for all of its internal logging. Will automatically add a correlation ID variable to the NLog
        /// thread context (<see cref="MappedDiagnosticsContext"/>) under the key specified by <paramref name="overriddenCorrelationIdPropertyKey"/> 
        /// when handling messages, allowing log output to include that.
        /// </summary>
        public static void Log4Net(this LoggingConfigurer configurer, string overriddenCorrelationIdPropertyKey)
        {
            configurer.Use(new NLogLoggerFactory());

            SetUpEventHandler(configurer, overriddenCorrelationIdPropertyKey);
        }

        static void SetUpEventHandler(BaseConfigurer configurer, string correlationIdPropertyKey)
        {
            configurer.Backbone.ConfigureEvents(e =>
            {
                e.BeforeTransportMessage +=
                    (bus, message) =>
                    {
                        var correlationid = message.Headers.ContainsKey(Headers.CorrelationId)
                                                ? message.Headers[Headers.CorrelationId]
                                                : "";

                        MappedDiagnosticsContext.Set(correlationIdPropertyKey, correlationid.ToString());
                    };
            });
        }
    }
}
