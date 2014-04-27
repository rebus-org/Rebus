using System;
using System.Collections.Generic;
using Rebus.Configuration;
using Rebus.Shared;
using Serilog;
using Serilog.Context;


namespace Rebus.Serilog
{
    /// <summary>
    /// Extensions to <see cref="LoggingConfigurer"/> that allows for choosing Serilog for all of Rebus' internal logging needs
    /// </summary>
    public static class SerilogLoggingExtension
    {
        /// <summary>
        /// Default Serilog context property key to use for setting the correlation ID of the message currently being handled.
        /// </summary>
        public const string DefaultCorrelationIdPropertyKey = "CorrelationId";

        /// <summary>
        /// Configures Rebus to use Serilog for all of its internal logging. Will automatically add a 'CorrelationId' variable as a Serilog
        /// context property when handling messages, allowing log output to include that.
        /// </summary>
        public static void Serilog(this LoggingConfigurer configurer)
        {
            configurer.Use(new SerilogLoggerFactory());

            SetUpEventHandler(configurer, DefaultCorrelationIdPropertyKey);
        }

        /// <summary>
        /// Configures Rebus to use Serilog for all of its internal logging. Will automatically add a correlation ID variable as a Serilog
        /// context property under the key specified by <paramref name="overriddenCorrelationIdPropertyKey"/> when handling messages, 
        /// allowing log output to include that.
        /// </summary>
        public static void Serilog(this LoggingConfigurer configurer, string overriddenCorrelationIdPropertyKey)
        {
            configurer.Use(new SerilogLoggerFactory());

            SetUpEventHandler(configurer, overriddenCorrelationIdPropertyKey);
        }

        [ThreadStatic]
        private static List<IDisposable> pushedProperties;

        static void SetUpEventHandler(BaseConfigurer configurer, string correlationIdPropertyKey)
        {
            configurer.Backbone.ConfigureEvents(e =>
                {
                    e.BeforeTransportMessage +=
                        (bus, message) =>
                            {
                                pushedProperties = new List<IDisposable>();

                                PushHeaderProperty(Headers.CorrelationId, message, correlationIdPropertyKey);
                                PushHeaderProperty(Headers.SourceQueue, message);
                                PushHeaderProperty(Headers.ReturnAddress, message);
                                PushHeaderProperty(Headers.AutoCorrelationSagaId, message);
                            };
                    e.AfterTransportMessage +=
                        (bus, exceptionOrNull, receivedTransportMessage) =>
                        {
                            if (pushedProperties != null)
                            {
                                foreach (IDisposable pushedProperty in pushedProperties)
                                    pushedProperty.Dispose();
                            }
                            pushedProperties = null;
                        };
                });
        }

        private static void PushHeaderProperty(string headerKey, ReceivedTransportMessage message,
            string serilogPropertyKey = null)
        {
            if (message.Headers.ContainsKey(headerKey))
            {
                if (string.IsNullOrEmpty(serilogPropertyKey))
                    serilogPropertyKey = headerKey;

                pushedProperties.Add(
                    LogContext.PushProperty(serilogPropertyKey, message.Headers[headerKey]));
            }
        }
    }
}