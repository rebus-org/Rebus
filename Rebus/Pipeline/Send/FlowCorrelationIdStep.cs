using System;
using System.Globalization;
using System.Threading.Tasks;
using Rebus.Extensions;
using Rebus.Messages;
using Rebus.Transport;

namespace Rebus.Pipeline.Send;

/// <summary>
/// Outgoing step that sets the <see cref="Headers.CorrelationId"/> header of the outgoing message if it has not already been set.
/// The value used is one of the following (in prioritized order):
/// 1) The correlation ID of the message currently being handled,
/// 2) The message ID of the message currently being handled,
/// 3) The message's own message ID
/// </summary>
[StepDocumentation($@"Sets the '{Headers.CorrelationId}' header of the outgoing message to one of the following three things:

1) The correlation ID of the message currently being handled.
2) The message ID of the message currently being handled.
3) The message's own message ID.")]
public class FlowCorrelationIdStep : IOutgoingStep
{
    /// <summary>
    /// Flows the correlation ID like it should
    /// </summary>
    public async Task Process(OutgoingStepContext context, Func<Task> next)
    {
        var outgoingMessage = context.Load<Message>();

        var transactionContext = context.Load<ITransactionContext>();
        var incomingStepContext = transactionContext.GetOrNull<IncomingStepContext>(StepContext.StepContextKey);

        if (!outgoingMessage.Headers.ContainsKey(Headers.CorrelationId))
        {
            var correlationInfo = GetCorrelationIdToAssign(incomingStepContext, outgoingMessage);

            outgoingMessage.Headers[Headers.CorrelationId] = correlationInfo.CorrelationId;
            outgoingMessage.Headers[Headers.CorrelationSequence] = correlationInfo.CorrelationSequence.ToString(CultureInfo.InvariantCulture);
        }

        await next();
    }

    static CorrelationInfo GetCorrelationIdToAssign(IncomingStepContext incomingStepContext, Message outgoingMessage)
    {
        // if we're handling an incoming message right now, let either current correlation ID or the message ID flow
        if (incomingStepContext == null)
        {
            var messageId = outgoingMessage.Headers.GetValue(Headers.MessageId);

            return new CorrelationInfo(messageId, 0);
        }

        var incomingMessage = incomingStepContext.Load<Message>();

        var correlationId = incomingMessage.Headers.GetValueOrNull(Headers.CorrelationId)
                            ?? incomingMessage.Headers.GetValue(Headers.MessageId);

        var correlationSequenceHeader = incomingMessage.Headers.GetValueOrNull(Headers.CorrelationSequence) ?? "0";
        var currentCorrelationSequence = ParseOrZero(correlationSequenceHeader);
        var nextCorrelationSequence = currentCorrelationSequence + 1;

        return new CorrelationInfo(correlationId, nextCorrelationSequence);
    }

    static int ParseOrZero(string correlationSequenceHeader)
    {
        try
        {
            return int.Parse(correlationSequenceHeader);
        }
        catch
        {
            return 0;
        }
    }

    struct CorrelationInfo
    {
        public CorrelationInfo(string correlationId, int correlationSequence)
        {
            CorrelationId = correlationId;
            CorrelationSequence = correlationSequence;
        }

        public string CorrelationId { get; }
        public int CorrelationSequence { get; }
    }
}