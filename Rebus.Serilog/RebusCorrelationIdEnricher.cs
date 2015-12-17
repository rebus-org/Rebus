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

        public RebusCorrelationIdEnricher(string propertyName)
        {
            _propertyName = propertyName;
        }

        public void Enrich(LogEvent logEvent, ILogEventPropertyFactory factory)
        {
            var messageContext = MessageContext.Current;
            if (messageContext == null) return;

            var correlationid = messageContext
                .TransportMessage.Headers
                .GetValueOrNull(Headers.CorrelationId);

            if (correlationid == null) return;

            logEvent.AddOrUpdateProperty(factory.CreateProperty(_propertyName, correlationid));
        }
    }
}