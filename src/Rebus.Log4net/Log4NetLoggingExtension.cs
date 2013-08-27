using Rebus.Configuration;
using Rebus.Shared;
using log4net;

namespace Rebus.Log4Net
{
    /// <summary>
    /// Extensions to <see cref="LoggingConfigurer"/> that allows for choosing Log4net for all of Rebus' internal logging needs
    /// </summary>
    public static class Log4NetLoggingExtension
    {
        static string correlationIdPropertyKey = "CorrelationId";

        /// <summary>
        /// Configures Rebus to use Log4net for all of its internal logging
        /// </summary>
        public static void Log4Net(this LoggingConfigurer configurer)
        {
            configurer.Use(new Log4NetLoggerFactory());

            SetUpEventHandler(configurer);
        }

        /// <summary>
        /// Configures Rebus to use Log4net for all of its internal logging
        /// </summary>
        public static void Log4Net(this LoggingConfigurer configurer, string correlationIdPropertyKey)
        {
            configurer.Use(new Log4NetLoggerFactory());

            SetUpEventHandler(configurer, correlationIdPropertyKey);
        }

        static void SetUpEventHandler(BaseConfigurer configurer, string overriddenCorrelationIdPropertyKey = null)
        {
            if (!string.IsNullOrWhiteSpace(overriddenCorrelationIdPropertyKey))
            {
                correlationIdPropertyKey = overriddenCorrelationIdPropertyKey;
            }

            configurer.Backbone.ConfigureEvents(e =>
                {
                    e.BeforeTransportMessage +=
                        (bus, message) =>
                            {
                                var correlationid = message.Headers.ContainsKey(Headers.CorrelationId)
                                                        ? message.Headers[Headers.CorrelationId]
                                                        : null;

                                ThreadContext.Properties[correlationIdPropertyKey] = correlationid;
                            };
                });
        }
    }
}