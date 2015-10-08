using System;
using System.Text;
using NLog;
using NLog.Config;
using NLog.LayoutRenderers;
using Rebus.Extensions;
using Rebus.Messages;
using Rebus.Pipeline;

namespace Rebus.NLog
{
    [LayoutRenderer(ItemName)]
    public class RebusCorrelationIdLayoutRenderer : LayoutRenderer
    {
        public const string ItemName = "rebus-correlation-id";

        /// <summary>
        /// Registers the Rebus correlation ID renderer under the <see cref="ItemName"/> key in the <see cref="ConfigurationItemFactory"/>
        /// found in <see cref="ConfigurationItemFactory.Default"/>
        /// </summary>
        public static void Register()
        {
            var configurationItemFactory = ConfigurationItemFactory.Default;

            Register(configurationItemFactory);
        }

        /// <summary>
        /// Registers the Rebus correlation ID renderer under the <see cref="ItemName"/> key in the given <see cref="ConfigurationItemFactory"/>
        /// </summary>
        public static void Register(ConfigurationItemFactory configurationItemFactory)
        {
            var namedItemFactory = configurationItemFactory.LayoutRenderers;

            Type dummy;
            var layoutRendererHasAlreadyBeenBeenRegistered = namedItemFactory
                .TryGetDefinition(ItemName, out dummy);

            if (layoutRendererHasAlreadyBeenBeenRegistered)
            {
                return;
            }

            namedItemFactory.RegisterDefinition(ItemName, typeof (RebusCorrelationIdLayoutRenderer));
        }

        protected override void Append(StringBuilder builder, LogEventInfo logEvent)
        {
            var context = MessageContext.Current;
            if (context == null) return;

            var correlationId = context
                .IncomingStepContext
                .Load<TransportMessage>()
                .Headers.GetValueOrNull(Headers.CorrelationId);

            if (correlationId == null) return;

            builder.Append(correlationId);
        }
    }
}