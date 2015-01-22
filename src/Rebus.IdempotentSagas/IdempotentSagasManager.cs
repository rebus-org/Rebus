using System;
using System.Collections.Generic;
using System.Linq;

using Ponder;

using Rebus;
using Rebus.Bus;
using Rebus.Messages;
using Rebus.Configuration;

namespace Rebus.IdempotentSagas
{
    /// <summary>
    /// Configuration extensions for configuring idempotent sagas support.
    /// </summary>
    public class IdempotentSagasManager
    {
        private ConfigurationBackbone _backbone = null;
        private static string sagaDataPropertyName = Reflect.Path<Saga<ISagaData>>(s => s.Data);

        internal IdempotentSagasManager(ConfigurationBackbone backbone)
        {
            if (backbone == null) throw new ArgumentNullException("backbone");

            _backbone = backbone;
            _backbone.ConfigureEvents(x => AttachEventHandlers(x));
        }

        /// <summary>
        /// Enables the ability to use IdempotentSagas.
        /// </summary>
        private IRebusEvents AttachEventHandlers(IRebusEvents events)
        {
            events.BeforeHandling += OnBeforeHandlingEvent;
            events.BeforeInternalSend += StoreHandlingSideEffects;
            events.BeforeInternalSend += RestoreMessageIdBeforeResend;
            events.AfterHandling += OnAfterHandlingEvent;

            return events;
        }

        /// <summary>
        /// Try to get idempotent's saga data, or return null if handler is not an idempotent saga handler.
        /// </summary>
        /// <param name="handler">The handler.</param>
        /// <returns></returns>
        private static IIdempotentSagaData TryGetSagaData(IHandleMessages handler)
        {
            if (!(handler is Saga)) return null;

            var sagaData = handler.GetType().GetProperty("Data");
            return sagaData.GetValue(handler, null) as IIdempotentSagaData;
        }

        /// <summary>
        /// Establishes the idempotent context for the specified message context.
        /// </summary>
        /// <param name="idempotentSagaData">The idempotent saga data.</param>
        /// <param name="messageContext">The message context.</param>
        private void EstablishIdempotencyContextFor(IIdempotentSagaData idempotentSagaData, IMessageContext messageContext)
        {
            var executionResults = new IdempotentSagaResults(messageContext.RebusTransportMessageId, messageContext.CurrentMessage);
            messageContext.Items.Add(Headers.IdempotentSagaResults, executionResults);
        }

        /// <summary>
        /// Stores any sent message during an idempotence message processing into a list which allows future re-playing.
        /// </summary>
        /// <param name="destinations">The destinations.</param>
        /// <param name="message">The message.</param>
        /// <param name="published">if set to <c>true</c> [published].</param>
        private void StoreHandlingSideEffects(IEnumerable<string> destinations, Message message, bool published)
        {
            if (MessageContext.HasCurrent)
            {
                var messageContext = MessageContext.GetCurrent();

                if (messageContext.Items.ContainsKey(Headers.IdempotentSagaResults))
                {
                    var handlingData = messageContext.Items[Headers.IdempotentSagaResults] as IdempotentSagaResults;

                    if (handlingData != null)
                    {
                        var serializer = _backbone.SerializeMessages;
                        var serializedMessage = serializer.Serialize(message);
                        handlingData.SideEffects.Add(
                            new IdempotentSagaResults.SideEffect(
                                destinations, serializedMessage.Headers, serializedMessage.Body, serializer.GetType()));
                    }
                }
            }
        }

        /// <summary>
        /// Sets up idemptency infraestructure before handling of message begins.
        /// </summary>
        /// <param name="bus">The bus.</param>
        /// <param name="message">The message.</param>
        /// <param name="handler">The handler.</param>
        private void OnBeforeHandlingEvent(IBus bus, object message, IHandleMessages handler)
        {
            var mcontext = MessageContext.GetCurrent();
            var idempotentSagaData = TryGetSagaData(handler);

            if (idempotentSagaData == null)
            {
                // Not idempotent? ok, do nothing..
                return;
            }

            // Create a list to store execution results and side effects.
            if (idempotentSagaData.ExecutionResults == null)
            {
                idempotentSagaData.ExecutionResults = new List<IdempotentSagaResults>();
            }

            // Let's see if we have a recorded results from a previous invocation.
            var handlingResults = idempotentSagaData.ExecutionResults.SingleOrDefault(x => x.Id == mcontext.RebusTransportMessageId);

            if (handlingResults == null)
            {
                // There's no record of previous handling of this message for this handler, so let's initialize our
                // idempotent saga-data storage, so our hooks can save any generated side-effects.
                EstablishIdempotencyContextFor(idempotentSagaData, mcontext);
            }
            else
            {
                // This is a message already handled, so we should avoid calling handlers, just re-apply any side effect and move on.
                ResendStoredSideEffects(bus, handlingResults, mcontext);

                // Avoid bus worker from handling (again) this message with current handler.
                mcontext.DoNotHandle = true;
            }

        }

        /// <summary>
        /// Tears down idemptency handling infraestructure for the current message & handler.
        /// </summary>
        /// <param name="bus">The bus.</param>
        /// <param name="message">The message.</param>
        /// <param name="handler">The handler.</param>
        private void OnAfterHandlingEvent(IBus bus, object message, IHandleMessages handler)
        {
            var idempotentSagaData = TryGetSagaData(handler);

            if (idempotentSagaData == null)
            {
                // Not idempotent? ok, do nothing..
                return;
            }

            var icontext = MessageContext.GetCurrent().Items[Headers.IdempotentSagaResults] as IdempotentSagaResults;

            if (icontext != null)
            {
                // Store results of current message handling into our idempotency store.
                idempotentSagaData.ExecutionResults.Add(icontext);
            }
        }

        /// <summary>
        /// Re-sends stored side effects from an already handled message.
        /// </summary>
        /// <param name="handlingData">The handling data.</param>
        /// <param name="bus">The bus.</param>
        /// <param name="messageContext">The message context.</param>
        private void ResendStoredSideEffects(IBus bus, IdempotentSagaResults handlingData, IMessageContext messageContext)
        {
            foreach (var item in handlingData.SideEffects)
            {
                // TODO: Ensure current serializer matches the one used during previous steps.

                var toSend = new TransportMessageToSend()
                {
                    Headers = item.Headers,
                    Body = item.Message
                };

                foreach (var destination in item.Destinations)
                {
                    _backbone.SendMessages.Send(destination, toSend, TransactionContext.Current);
                }

#if false
                foreach (var outgoingMessage in item.Messages)
                {
                    var deserializedMessage = JsonConvert.DeserializeObject(outgoingMessage.Value, Type.GetType(outgoingMessage.Key));

                    foreach (var header in item.Headers)
                    {
                        if (header.Key == Rebus.Shared.Headers.MessageId)
                        {
                            bus.AttachHeader(deserializedMessage, Headers.OriginalMessageId, header.Value.ToString());
                            continue;
                        }

                        bus.AttachHeader(deserializedMessage, header.Key, header.Value.ToString());
                    }

                    foreach (var destination in item.Destinations)
                    {
                        bus.Advanced.Routing.Send(destination, deserializedMessage);
                    }
                }
#endif
            }
        }

        /// <summary>
        /// Fixes up the message id before resending.
        /// </summary>
        /// <param name="destinations">The destinations.</param>
        /// <param name="message">The message.</param>
        /// <param name="published">if set to <c>true</c> [published].</param>
        /// <remarks>
        /// This is needed by ResendStoredSideEffects in order to send messages using the original message-id, 
        /// as otherwise, the message-id get's overwritten by rebus's internals.
        /// </remarks>
        private void RestoreMessageIdBeforeResend(IEnumerable<string> destinations, Message message, bool published)
        {
            if (message.Headers.ContainsKey(Headers.OriginalMessageId))
            {
                // If we are replaying a message, use original message id.
                message.Headers[Rebus.Shared.Headers.MessageId] = message.Headers[Headers.OriginalMessageId];
                message.Headers.Remove(Headers.OriginalMessageId);
            }
        }
    }
}
