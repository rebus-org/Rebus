using System;
using System.Threading.Tasks;
using Rebus.Bus;
using Rebus.Messages;
using Rebus.Pipeline;
using Rebus.Time;
using Rebus.Transport;

namespace Rebus.Auditing
{
    /// <summary>
    /// Implementation of <see cref="IIncomingStep"/> and <see cref="IOutgoingStep"/> that handles message auditing
    /// </summary>
    [StepDocumentation("Forwards a copy of published messages to the configured audit queue, including some useful headers.")]
    class OutgoingAuditingStep : IOutgoingStep, IInitializable
    {
        readonly string _auditQueue;
        readonly ITransport _transport;

        /// <summary>
        /// Constructs the step
        /// </summary>
        public OutgoingAuditingStep(string auditQueue, ITransport transport)
        {
            _auditQueue = auditQueue;
            _transport = transport;
        }

        public void Initialize()
        {
            _transport.CreateQueue(_auditQueue);
        }

        public async Task Process(OutgoingStepContext context, Func<Task> next)
        {
            var transportMessage = context.Load<TransportMessage>();

            if (IsPublishedMessage(transportMessage))
            {
                var transactionContext = context.Load<ITransactionContext>();

                var clone = transportMessage.Clone();
                clone.Headers[AuditHeaders.AuditTime] = RebusTime.Now.ToString("O");

                await _transport.Send(_auditQueue, clone, transactionContext);
            }

            await next();
        }

        static bool IsPublishedMessage(TransportMessage transportMessage)
        {
            string intent;
            return transportMessage.Headers.TryGetValue(Headers.Intent, out intent)
                   && intent == Headers.IntentOptions.PublishSubscribe;
        }
    }
}