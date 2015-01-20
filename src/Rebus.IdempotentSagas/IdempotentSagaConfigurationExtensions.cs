using System;
using System.Collections.Generic;
using System.Linq;

using Newtonsoft.Json;
using Ponder;

using Rebus.Messages;

namespace Rebus.IdempotentSagas
{
    /// <summary>
    /// Configuration extensions for configuring idempotent sagas support.
    /// </summary>
    public static class IdempotentSagaConfigurationExtensions
    {
        private static string sagaDataPropertyName = Reflect.Path<Saga<ISagaData>>(s => s.Data);

        private static IIdempotentSagaData TryGetSagaData(IHandleMessages handler)
        {
            if (!(handler is Saga)) return null;

            var sagaData = handler.GetType().GetProperty("Data");
            return sagaData.GetValue(handler, null) as IIdempotentSagaData;
        }

        /// <summary>
        /// Stores any sent message during an idempotence message processing into a list which allows future re-playing.
        /// </summary>
        /// <param name="destinations">The destinations.</param>
        /// <param name="message">The message.</param>
        /// <param name="published">if set to <c>true</c> [published].</param>
        private static void StoreIdempotenceMessageSideEffects(IEnumerable<string> destinations, Message message, bool published)
        {
            if (MessageContext.HasCurrent)
            {
                var messageContext = MessageContext.GetCurrent();

                if (messageContext.Items.ContainsKey(Headers.IdempotentSagaContext))
                {
                    var handlingData = messageContext.Items[Headers.IdempotentSagaContext] as IdempotentMessageData;

                    if (handlingData != null)
                    {
                        var serializedMessages = message.Messages.ToDictionary(x => x.GetType().AssemblyQualifiedName, x => JsonConvert.SerializeObject(x));
                        handlingData.SideEffects.Add(new IdempotentMessageData.SideEffect(destinations, message.Headers, serializedMessages));
                    }
                }
            }
        }

        /// <summary>
        /// Fixups the message id before replaying it.
        /// </summary>
        /// <param name="destinations">The destinations.</param>
        /// <param name="message">The message.</param>
        /// <param name="published">if set to <c>true</c> [published].</param>
        private static void FixupMessageIdBeforeReplay(IEnumerable<string> destinations, Message message, bool published)
        {
            if (message.Headers.ContainsKey(Headers.ReplayMessageId))
            {
                // If we are replaying a message, use original message id.
                message.Headers[Rebus.Shared.Headers.MessageId] = message.Headers[Headers.ReplayMessageId];
                message.Headers.Remove(Headers.ReplayMessageId);
            }
        }

        private static void BeforeHandlingEvent(IBus bus, object message, IHandleMessages handler)
        {
            var idempotentSagaData = TryGetSagaData(handler);

            if (idempotentSagaData == null)
            {
                // Not idempotent? ok, do nothing..
                return;
            }

            if (idempotentSagaData.HandledMessages == null)
            {
                idempotentSagaData.HandledMessages = new List<IdempotentMessageData>();
            }

            var handlingData = idempotentSagaData.HandledMessages.SingleOrDefault(x => x.Id == MessageContext.GetCurrent().RebusTransportMessageId);

            if (handlingData == null)
            {
                // There's no record of previous handling of this message for this handler, so let's initialize our
                // idempotent saga-data storage, so our hooks can save any generated side-effects.

                handlingData = new IdempotentMessageData(MessageContext.GetCurrent().RebusTransportMessageId, message);
                MessageContext.GetCurrent().Items.Add(Headers.IdempotentSagaContext, handlingData);
            }
            else
            {
                // This is a message already handled, so we should avoid calling handlers, just re-apply any side effect and move on.

                MessageContext.GetCurrent().DoNotHandle = true;

                foreach (var item in handlingData.SideEffects)
                {
                    foreach (var outgoingMessage in item.Messages)
                    {
                        var deserializedMessage = JsonConvert.DeserializeObject(outgoingMessage.Value, Type.GetType(outgoingMessage.Key));

                        foreach (var header in item.Headers)
                        {
                            if (header.Key == Rebus.Shared.Headers.MessageId)
                            {
                                bus.AttachHeader(deserializedMessage, Headers.ReplayMessageId, header.Value.ToString());
                                continue;
                            }

                            bus.AttachHeader(deserializedMessage, header.Key, header.Value.ToString());
                        }

                        foreach (var destination in item.Destinations)
                        {
                            bus.Advanced.Routing.Send(destination, deserializedMessage);
                        }
                    }

                }
            }

        }

        private static void AfterHandlingEvent(IBus bus, object message, IHandleMessages handler)
        {
            var idempotentSagaData = TryGetSagaData(handler);

            if (idempotentSagaData == null)
            {
                // Not idempotent? ok, do nothing..
                return;
            }

            var context = MessageContext.GetCurrent().Items[Headers.IdempotentSagaContext] as IdempotentMessageData;

            if (context != null)
            {
                // Update sagadata with IdempotentContext
                idempotentSagaData.HandledMessages.Add(context);
            }
        }

        /// <summary>
        /// Enables the ability to use IDempotentSagas.
        /// </summary>
        public static IRebusEvents ConfigureIdempotentSagas(this IRebusEvents events)
        {
            events.BeforeInternalSend += StoreIdempotenceMessageSideEffects;
            events.BeforeInternalSend += FixupMessageIdBeforeReplay;
            events.BeforeHandling += BeforeHandlingEvent;
            events.AfterHandling += AfterHandlingEvent;

            return events;
        }
    }
}
