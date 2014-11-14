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

        private static void BeforeInternalSendEvent(IEnumerable<string> destinations, Message message, bool published)
        {
            if (message.Headers.ContainsKey(Headers.ReplayMessageId))
            {
                // If we are replaying a message, use original message id.
                message.Headers[Rebus.Shared.Headers.MessageId] = message.Headers[Headers.ReplayMessageId];
                message.Headers.Remove(Headers.ReplayMessageId);
            }
            else if (MessageContext.HasCurrent)
            {
                var messageContext = MessageContext.GetCurrent();
                if (messageContext.Items.ContainsKey(SagaContext.SagaContextItemKey))
                {
                    if (messageContext.Items.ContainsKey(Headers.IdempotentSagaContext))
                    {
                        var processingMessage = messageContext.Items[Headers.IdempotentSagaContext] as ProcessedMessage;

                        if (processingMessage != null)
                        {
                            var serializedMessages = message.Messages.ToDictionary(x => x.GetType().AssemblyQualifiedName, x => JsonConvert.SerializeObject(x));

                            processingMessage.Consecuences.Add(new ProcessedMessage.Consecuence()
                            {
                                Destinations = destinations,
                                Headers = message.Headers,
                                Messages = serializedMessages,
                            });
                        }
                    }
                }
            }
        }
        
        private static void BeforeHandlingEvent(IBus bus, object message, IHandleMessages handler)
        {
            if (handler is Saga)
            {
                var idempotentSagaData = handler.GetType().GetProperty("Data").GetValue(handler, null) as IIdempotentSagaData;

                if (idempotentSagaData == null)
                {
                    return;
                }

                if (idempotentSagaData.ProcessedMessages == null)
                {
                    idempotentSagaData.ProcessedMessages = new List<ProcessedMessage>();
                }

                var processedMessage = idempotentSagaData.ProcessedMessages.SingleOrDefault(x => x.Id == MessageContext.GetCurrent().RebusTransportMessageId);

                if (processedMessage != null)
                {
                    MessageContext.GetCurrent().DoNotHandle = true;

                    foreach (var consecuence in processedMessage.Consecuences)
                    {
                        foreach (var outgoingMessage in consecuence.Messages)
                        {
                            var deserializedMessage = JsonConvert.DeserializeObject(outgoingMessage.Value, Type.GetType(outgoingMessage.Key));

                            foreach (var header in consecuence.Headers)
                            {
                                if (header.Key == Rebus.Shared.Headers.MessageId)
                                {
                                    bus.AttachHeader(deserializedMessage, Headers.ReplayMessageId, header.Value.ToString());
                                    continue;
                                }

                                bus.AttachHeader(deserializedMessage, header.Key, header.Value.ToString());
                            }

                            foreach (var destination in consecuence.Destinations)
                            {
                                bus.Advanced.Routing.Send(destination, deserializedMessage);
                            }
                        }

                    }
                }
                else
                {
                    // Initialize IdempotentContext
                    processedMessage = new ProcessedMessage()
                    {
                        Id = MessageContext.GetCurrent().RebusTransportMessageId,
                        Message = message
                    };

                    MessageContext.GetCurrent().Items.Add(Headers.IdempotentSagaContext, processedMessage);
                }
            }
        }

        private static void AfterHandlingEvent(IBus bus, object message, IHandleMessages handler)
        {
            if (handler is Saga)
            {
                var idempotentSagaData = handler.GetType().GetProperty("Data").GetValue(handler, null) as IIdempotentSagaData;
                if (idempotentSagaData != null)
                {
                    var context = MessageContext.GetCurrent().Items[Headers.IdempotentSagaContext] as ProcessedMessage;

                    if (context != null)
                    {
                        // Update sagadata with IdempotentContext
                        idempotentSagaData.ProcessedMessages.Add(context);
                    }
                }
            }
        }

        /// <summary>
        /// Enables the ability to use IDempotentSagas.
        /// </summary>
        public static IRebusEvents ConfigureIdempotentSagas(this IRebusEvents events)
        {
            events.BeforeInternalSend += BeforeInternalSendEvent;
            events.BeforeHandling += BeforeHandlingEvent;
            events.AfterHandling += AfterHandlingEvent;

            return events;
        }
    }
}
