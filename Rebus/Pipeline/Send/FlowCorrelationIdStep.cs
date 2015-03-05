using System;
using System.Threading.Tasks;
using Rebus.Extensions;
using Rebus.Messages;
using Rebus.Transport;

namespace Rebus.Pipeline.Send
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