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
        /// <summary>
        /// Default Log4Net thread context key to use for setting the correlation ID of the message currently being handled.
        /// </summary>
        public const string DefaultCorrelationIdPropertyKey = "CorrelationId";

        /// <summary>
        /// Configures Rebus to use Log4net for all of its internal logging. Will automatically add a 'CorrelationId' variable to the Log4Net
        /// thread context when handling messages, allowing log output to include that.
        /// </summary>
        public static void Log4Net(this LoggingConfigurer configurer)
        {
            configurer.Use(new Log4NetLoggerFactory());

            SetUpEventHandler(configurer, DefaultCorrelationIdPropertyKey);
        }

        /// <summary>
        /// Configures Rebus to use Log4net for all of its internal logging. Will automatically add a correlation ID variable to the Log4Net
        /// thread context under the key specified by <paramref name="overriddenCorrelationIdPropertyKey"/> when handling messages, 
        /// allowing log output to include that.
        /// </summary>
        public static void Log4Net(this LoggingConfigurer configurer, string overriddenCorrelationIdPropertyKey)
        {
            configurer.Use(new Log4NetLoggerFactory());

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
                                                        : null;

                                ThreadContext.Properties[correlationIdPropertyKey] = correlationid;
                            };
                });
        }
    }
}