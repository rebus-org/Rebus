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
    [StepDocumentation("Wraps the execution of the entire receive pipeline and forwards a copy of the current transport message to the configured audit queue if processing was successful, including some useful headers.")]
    class IncomingAuditingStep : IIncomingStep, IInitializable
    {
        readonly string _auditQueue;
        readonly ITransport _transport;

        /// <summary>
        /// Constructs the step
        /// </summary>
        public IncomingAuditingStep(string auditQueue, ITransport transport)
        {
            _auditQueue = auditQueue;
            _transport = transport;
        }

        public void Initialize()
        {
            _transport.CreateQueue(_auditQueue);
        }

        public async Task Process(IncomingStepContext context, Func<Task> next)
        {
            var begin = RebusTime.Now;

            await next();

            var transactionContext = context.Load<ITransactionContext>();
            var transportMessage = context.Load<TransportMessage>();

            var clone = transportMessage.Clone();
            clone.Headers[AuditHeaders.HandleTime] = begin.ToString("O");
            clone.Headers[AuditHeaders.AuditTime] = RebusTime.Now.ToString("O");
            clone.Headers[AuditHeaders.HandleQueue] = _transport.Address;

            await _transport.Send(_auditQueue, clone, transactionContext);
        }
    }
}