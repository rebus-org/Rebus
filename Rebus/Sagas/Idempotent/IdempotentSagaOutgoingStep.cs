using System;
using System.Threading.Tasks;
using Rebus.Bus;
using Rebus.Extensions;
using Rebus.Messages;
using Rebus.Pipeline;
using Rebus.Pipeline.Receive;
using Rebus.Pipeline.Send;
using Rebus.Transport;

namespace Rebus.Sagas.Idempotent
{
    public class IdempotentSagaOutgoingStep : IOutgoingStep
    {
        public async Task Process(OutgoingStepContext context, Func<Task> next)
        {
            var transactionContext = context.Load<ITransactionContext>();

            object temp;
            if (transactionContext.Items.TryGetValue(HandlerInvoker.CurrentHandlerInvokerItemsKey, out temp))
            {
                var handlerInvoker = (HandlerInvoker)temp;

                if (handlerInvoker.HasSaga)
                {
                    var idempotentSagaData = handlerInvoker.GetSagaData() as IIdempotentSagaData;

                    if (idempotentSagaData != null)
                    {
                        var idempotencyData = idempotentSagaData.IdempotencyData;

                        var transportMessage = context.Load<TransportMessage>();
                        var destinationAddresses = context.Load<DestinationAddresses>();
                        var incomingStepContext = transactionContext.Items.GetOrThrow<IncomingStepContext>(StepContext.StepContextKey);
                        var messageId = incomingStepContext.Load<Message>().GetMessageId();

                        idempotencyData.StoreOutgoingMessage(messageId, destinationAddresses, transportMessage);
                    }
                }
            }

            await next();
        }
    }
}