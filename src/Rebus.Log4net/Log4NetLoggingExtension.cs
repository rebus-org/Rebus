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
        /// Configures Rebus to use Log4net for all of its internal logging
        /// </summary>
        public static void Log4Net(this LoggingConfigurer configurer)
        {
            configurer.Use(new Log4NetLoggerFactory());
        }

        /// <summary>
        /// Installs a hook that automatically transfers the correlation ID of incoming messages to the Log4Net
        /// <see cref="ThreadContext"/>, allowing it to be included in the logging output. <see cref="propertyKey"/>
        /// specifies the key under which the correlation ID will be set.
        /// </summary>
        public static RebusConfigurer TransferCorrelationIdToLog4NetThreadContext(this RebusConfigurer configurer, string propertyKey)
        {
            configurer.Events(e => SetLoggerPropertiesWhenAvailable(e, propertyKey));

            return configurer;
        }

        /// <summary>
        /// Installs a hook that automatically transfers the correlation ID of incoming messages to the Log4Net
        /// <see cref="ThreadContext"/> under the default key 'CorrelationId', allowing it to be included in the logging output.
        /// </summary>
        public static RebusConfigurer TransferCorrelationIdToLog4NetThreadContext(this RebusConfigurer configurer)
        {
            return TransferCorrelationIdToLog4NetThreadContext(configurer, "CorrelationId");
        }

        static void SetLoggerPropertiesWhenAvailable(IRebusEvents e, string propertyKey)
        {
            e.BeforeTransportMessage +=
                (bus, message) =>
                {
                    var correlationid = message.Headers.ContainsKey(Headers.CorrelationId)
                                            ? message.Headers[Headers.CorrelationId]
                                            : null;

                    ThreadContext.Properties[propertyKey] = correlationid;
                };
        }
    }
}