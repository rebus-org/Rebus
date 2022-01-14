using System;
using System.Threading.Tasks;
using Rebus.Bus;
using Rebus.Messages;
using Rebus.Pipeline;
using Rebus.Transport;

namespace Rebus.Auditing.Messages;

/// <summary>
/// Implementation of <see cref="IIncomingStep"/> and <see cref="IOutgoingStep"/> that handles message auditing
/// </summary>
[StepDocumentation("Forwards a copy of published messages to the configured audit queue, including some useful headers.")]
class OutgoingAuditingStep : IOutgoingStep, IInitializable
{
    readonly AuditingHelper _auditingHelper;
    readonly ITransport _transport;

    /// <summary>
    /// Constructs the step
    /// </summary>
    public OutgoingAuditingStep(AuditingHelper auditingHelper, ITransport transport)
    {
        _auditingHelper = auditingHelper;
        _transport = transport;
    }

    public void Initialize()
    {
        _auditingHelper.EnsureAuditQueueHasBeenCreated();
    }

    public async Task Process(OutgoingStepContext context, Func<Task> next)
    {
        var transportMessage = context.Load<TransportMessage>();

        if (IsPublishedMessage(transportMessage))
        {
            var transactionContext = context.Load<ITransactionContext>();

            var clone = transportMessage.Clone();

            _auditingHelper.SetCommonHeaders(clone);

            await _transport.Send(_auditingHelper.AuditQueue, clone, transactionContext);
        }

        await next();
    }

    static bool IsPublishedMessage(TransportMessage transportMessage)
    {
        return transportMessage.Headers.TryGetValue(Headers.Intent, out var intent)
               && intent == Headers.IntentOptions.PublishSubscribe;
    }
}