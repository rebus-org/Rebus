using System.Collections.Generic;
using System.Linq;
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
        public const string CorrelationIdLoggerProperty = "CorrelationId";

        static readonly Dictionary<string, string> DefaultHeaderKeyToLoggerPropertyMappings =
            new Dictionary<string, string>
            {
                {Headers.CorrelationId, CorrelationIdLoggerProperty}
            };

        /// <summary>
        /// Configures Rebus to use Serilog for all of its internal logging. Automatically adds the <see cref="CorrelationIdLoggerProperty"/> 
        /// logger property to the logging context when handling a message that has a <see cref="Headers.CorrelationId"/> header.
        /// </summary>
        public static void Serilog(this LoggingConfigurer configurer)
        {
            configurer.Use(new SerilogLoggerFactory());

            SetUpEventHandler(configurer, DefaultHeaderKeyToLoggerPropertyMappings);
        }

        /// <summary>
        /// Configures Rebus to use Serilog for all of its internal logging. Will add as logger properties the headers specified
        /// as keys in the given <see cref="headerKeyToLoggerPropertyMappings"/> dictionary, using as logger property name the
        /// value for each given key.
        /// </summary>
        public static void Serilog(this LoggingConfigurer configurer, IDictionary<string,string> headerKeyToLoggerPropertyMappings)
        {
            configurer.Use(new SerilogLoggerFactory());

            SetUpEventHandler(configurer, headerKeyToLoggerPropertyMappings);
        }

        /// <summary>
        /// Configures Rebus to use Serilog for all of its internal logging, deriving its logger off of the given <see cref="baseLogger"/>.
        ///  Automatically adds the <see cref="CorrelationIdLoggerProperty"/> 
        /// logger property to the logging context when handling a message that has a <see cref="Headers.CorrelationId"/> header.
        /// </summary>
        public static void Serilog(this LoggingConfigurer configurer, ILogger baseLogger)
        {
            configurer.Use(new SerilogLoggerFactory(baseLogger));

            SetUpEventHandler(configurer, DefaultHeaderKeyToLoggerPropertyMappings);
        }

        /// <summary>
        /// Configures Rebus to use Serilog for all of its internal logging, deriving its logger off of the given <see cref="baseLogger"/>.
        /// Will add as logger properties the headers specified
        /// as keys in the given <see cref="headerKeyToLoggerPropertyMappings"/> dictionary, using as logger property name the
        /// value for each given key.
        /// </summary>
        public static void Serilog(this LoggingConfigurer configurer, ILogger baseLogger, IDictionary<string,string> headerKeyToLoggerPropertyMappings)
        {
            configurer.Use(new SerilogLoggerFactory(baseLogger));

            SetUpEventHandler(configurer, headerKeyToLoggerPropertyMappings);
        }       
        
        /// <summary>
        /// Configures Rebus to use Serilog for all of its internal logging, using the given <see cref="configuration"/> to create
        /// all of its loggers. Automatically adds the <see cref="CorrelationIdLoggerProperty"/> 
        /// logger property to the logging context when handling a message that has a <see cref="Headers.CorrelationId"/> header.
        /// </summary>
        public static void Serilog(this LoggingConfigurer configurer, LoggerConfiguration configuration)
        {
            configurer.Use(new SerilogLoggerFactory(configuration));

            SetUpEventHandler(configurer, DefaultHeaderKeyToLoggerPropertyMappings);
        }

        /// <summary>
        /// Configures Rebus to use Serilog for all of its internal logging, using the given <see cref="configuration"/> to create
        /// all of its loggers. Will add as logger properties the headers specified as keys in the given <see cref="headerKeyToLoggerPropertyMappings"/> 
        /// dictionary, using as logger property name the value for each given key.
        /// </summary>
        public static void Serilog(this LoggingConfigurer configurer, LoggerConfiguration configuration, IDictionary<string, string> headerKeyToLoggerPropertyMappings)
        {
            configurer.Use(new SerilogLoggerFactory(configuration));

            SetUpEventHandler(configurer, headerKeyToLoggerPropertyMappings);
        }

        static void SetUpEventHandler(BaseConfigurer configurer, IDictionary<string, string> headerKeyToLoggerPropertyMappings)
        {
            configurer.Backbone.ConfigureEvents(e =>
                {
                    e.MessageContextEstablished += 
                        (bus, ctx) => 
                            {
                                foreach (var key in ctx.Headers.Keys.Intersect(headerKeyToLoggerPropertyMappings.Keys))
                                {
                                    PushHeaderProperty(key, ctx, headerKeyToLoggerPropertyMappings[key]);
                                }
                            };
                });
        }

        static void PushHeaderProperty(string headerKey, IMessageContext ctx, string loggerKey)
        {
            var propertyDisposer = LogContext.PushProperty(loggerKey, ctx.Headers[headerKey]);

            ctx.Disposed += propertyDisposer.Dispose;
        }
    }
}