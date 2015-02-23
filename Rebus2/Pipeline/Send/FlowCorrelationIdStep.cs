using System;
using System.Threading.Tasks;
using Rebus2.Extensions;
using Rebus2.Messages;
using Rebus2.Transport;

namespace Rebus2.Pipeline.Send
{
    public class FlowCorrelationIdStep : IOutgoingStep
    {
        public async Task Process(OutgoingStepContext context, Func<Task> next)
        {
            var outgoingMessage = context.Load<Message>();

            var transactionContext = context.Load<ITransactionContext>();
            var incomingStepContext = transactionContext.Items.GetOrNull<IncomingStepContext>(StepContext.StepContextKey);

            if (!outgoingMessage.Headers.ContainsKey(Headers.CorrelationId))
            {
                var correlationId = GetCorrelationIdToAssign(incomingStepContext, outgoingMessage);

                outgoingMessage.Headers[Headers.CorrelationId] = correlationId;
            }

            await next();
        }

        string GetCorrelationIdToAssign(IncomingStepContext incomingStepContext, Message outgoingMessage)
        {
            // if we're handling an incoming message right now, let either current correlation ID or the message ID flow
            if (incomingStepContext != null)
            {
                var incomingMessage = incomingStepContext.Load<Message>();

                var correlationId = incomingMessage.Headers.GetValueOrNull(Headers.CorrelationId)
                                    ?? incomingMessage.Headers.GetValue(Headers.MessageId);

                return correlationId;
            }
            else
            {
                // otherwise, use the current message ID as the correlation ID
                return outgoingMessage.Headers.GetValue(Headers.MessageId);
            }

        }
    }
}