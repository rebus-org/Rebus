using System;
using Rebus.Extensions;
using Rebus.Messages;
using Rebus.Pipeline;
using Serilog.Core;
using Serilog.Events;

namespace Rebus.Serilog
{
    /// <summary>
    /// Serilog log event enricher that adds Rebus' correlation ID to log events when called inside a Rebus message handler.
    /// Relies on <see cref="MessageContext.Current"/> being present - does not change the log line if it is not.
    /// </summary>
    public class RebusCorrelationIdEnricher : ILogEventEnricher
    {
        readonly string _propertyName;

        /// <summary>
        /// Creates the enricher, using the specified <paramref name="propertyName"/> as the name of the field
        /// </summary>
        public RebusCorrelationIdEnricher(string propertyName)
        {
            if (propertyName == null) throw new ArgumentNullException(nameof(propertyName));
            _propertyName = propertyName;
        }

        /// Enriches the <paramref name="logEvent"/> with a log event property that has the correlation ID
        /// of the message currently being handled if possible. Otherwise, no property is added.
        public void Enrich(LogEvent logEvent, ILogEventPropertyFactory factory)
        {
            var messageContext = MessageContext.Current;

            var correlationid = messageContext?.TransportMessage.Headers
                .GetValueOrNull(Headers.CorrelationId);

            if (correlationid == null) return;

            var logEventProperty = factory.CreateProperty(_propertyName, correlationid);

            logEvent.AddOrUpdateProperty(logEventProperty);
        }
    }
}