using Rebus.Extensions;
using Rebus.Messages;
using Rebus.Pipeline;
using Rebus.Transport;
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
            var transactionContext = AmbientTransactionContext.Current;
            var outgoingStepContext = transactionContext?.GetOrNull<OutgoingStepContext>("outgoingStepContext");
            var transportMessage = outgoingStepContext?.Load<TransportMessage>();

            if (transportMessage == null)
            {
                var messageContext = MessageContext.Current;
                transportMessage = messageContext?.TransportMessage;
            }

            var correlationid = transportMessage?.Headers.GetValueOrNull(Headers.CorrelationId);

            if (correlationid == null) return;

            logEvent.AddOrUpdateProperty(factory.CreateProperty(_propertyName, correlationid));
        }
    }
}