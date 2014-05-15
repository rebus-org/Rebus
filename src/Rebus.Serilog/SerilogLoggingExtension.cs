using Rebus.Configuration;
using Rebus.Shared;
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

        static void SetUpEventHandler(BaseConfigurer configurer, string correlationIdPropertyKey)
        {
            configurer.Backbone.ConfigureEvents(e =>
                {
                    e.MessageContextEstablished += 
                        (bus, ctx) => 
                            {
                                PushHeaderProperty(Headers.CorrelationId, ctx, correlationIdPropertyKey);
                                PushHeaderProperty(Headers.SourceQueue, ctx);
                                PushHeaderProperty(Headers.ReturnAddress, ctx);
                                PushHeaderProperty(Headers.AutoCorrelationSagaId, ctx);
                            };
                });
        }

        static void PushHeaderProperty(string headerKey, IMessageContext ctx, string serilogPropertyKey = null)
        {
            if (!ctx.Headers.ContainsKey(headerKey)) return;

            if (string.IsNullOrEmpty(serilogPropertyKey))
            {
                serilogPropertyKey = headerKey;
            }

            ctx.Disposed += LogContext.PushProperty(serilogPropertyKey, ctx.Headers[headerKey]).Dispose;
        }
    }
}