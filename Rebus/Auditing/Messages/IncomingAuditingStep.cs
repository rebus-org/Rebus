using System;
using System.Threading.Tasks;
using Rebus.Bus;
using Rebus.Messages;
using Rebus.Pipeline;
using Rebus.Time;
using Rebus.Transport;

namespace Rebus.Auditing.Messages;

/// <summary>
/// Implementation of <see cref="IIncomingStep"/> and <see cref="IOutgoingStep"/> that handles message auditing
/// </summary>
[StepDocumentation("Wraps the execution of the entire receive pipeline and forwards a copy of the current transport message to the configured audit queue if processing was successful, including some useful headers.")]
class IncomingAuditingStep : IIncomingStep, IInitializable
{
    readonly AuditingHelper _auditingHelper;
    readonly ITransport _transport;
    readonly IRebusTime _rebusTime;

    /// <summary>
    /// Constructs the step
    /// </summary>
    public IncomingAuditingStep(AuditingHelper auditingHelper, ITransport transport, IRebusTime rebusTime)
    {
        _auditingHelper = auditingHelper ?? throw new ArgumentNullException(nameof(auditingHelper));
        _transport = transport ?? throw new ArgumentNullException(nameof(transport));
        _rebusTime = rebusTime ?? throw new ArgumentNullException(nameof(rebusTime));
    }

    public void Initialize()
    {
        _auditingHelper.EnsureAuditQueueHasBeenCreated();
    }

    public async Task Process(IncomingStepContext context, Func<Task> next)
    {
        var begin = _rebusTime.Now;

        await next();

        var transactionContext = context.Load<ITransactionContext>();
        var transportMessage = context.Load<TransportMessage>();

        var clone = transportMessage.Clone();
            
        _auditingHelper.SetCommonHeaders(clone);

        clone.Headers[AuditHeaders.HandleTime] = begin.ToString("O");

        await _transport.Send(_auditingHelper.AuditQueueName, clone, transactionContext);
    }
}