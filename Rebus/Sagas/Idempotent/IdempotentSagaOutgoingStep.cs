using System;
using System.Threading.Tasks;
using Rebus.Bus;
using Rebus.Extensions;
using Rebus.Messages;
using Rebus.Pipeline;
using Rebus.Pipeline.Receive;
using Rebus.Pipeline.Send;
using Rebus.Transport;

namespace Rebus.Sagas.Idempotent;

/// <summary>
/// Outgoing pipeline step that stores the sent message in the current saga data (if it is an <see cref="IIdempotentSagaData"/>)
/// </summary>
[StepDocumentation(@"If the sent message originates from an idempotent saga, the message is stored in the saga's IdempotencyData in order to allow for re-sending it later on if necessary.")]
public class IdempotentSagaOutgoingStep : IOutgoingStep
{
    /// <summary>
    /// Carries out whichever logic it takes to do something good for the outgoing message :)
    /// </summary>
    public async Task Process(OutgoingStepContext context, Func<Task> next)
    {
        var transactionContext = context.Load<ITransactionContext>();
        var items = transactionContext.Items;

        if (items.TryGetValue(HandlerInvoker.CurrentHandlerInvokerItemsKey, out var item))
        {
            if (item is HandlerInvoker handlerInvoker)
            {
                if (handlerInvoker.HasSaga)
                {
                    if (handlerInvoker.GetSagaData() is IIdempotentSagaData idempotentSagaData)
                    {
                        var idempotencyData = idempotentSagaData.IdempotencyData;

                        var transportMessage = context.Load<TransportMessage>();
                        var destinationAddresses = context.Load<DestinationAddresses>();
                        var incomingStepContext = items.GetOrThrow<IncomingStepContext>(StepContext.StepContextKey);
                        var messageId = incomingStepContext.Load<Message>().GetMessageId();

                        idempotencyData.AddOutgoingMessage(messageId, destinationAddresses, transportMessage);
                    }
                }
            }
        }

        await next();
    }
}