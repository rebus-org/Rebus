using System;
using System.Collections.Generic;
using System.Linq;
using Rebus.Bus;
using Rebus.Configuration;
using Rebus.Logging;
using Rebus.Messages;

namespace Rebus.IdempotentSagas
{
    /// <summary>
    /// Configuration extensions for configuring idempotent sagas support.
    /// </summary>
    public class IdempotentSagasManager
    {
        static ILog log;
        readonly ConfigurationBackbone backbone;

        internal IdempotentSagasManager(ConfigurationBackbone backbone)
        {
            if (backbone == null) throw new ArgumentNullException("backbone");

            this.backbone = backbone;
            RebusLoggerFactory.Changed += f => log = f.GetCurrentClassLogger();
            this.backbone.ConfigureEvents(x => AttachEventHandlers(x));
        }

        /// <summary>
        /// Enables the ability to use IdempotentSagas.
        /// </summary>
        private IRebusEvents AttachEventHandlers(IRebusEvents events)
        {
            events.BeforeHandling += OnBeforeHandlingEvent;
            events.BeforeInternalSend += StoreHandlingSideEffects;
            events.AfterHandling += OnAfterHandlingEvent;
            events.OnHandlingError += OnHandlingError;

            return events;
        }

        /// <summary>
        /// Removes the idempotency context.
        /// </summary>
        private void RemoveIdempotencyContext()
        {
            if (MessageContext.HasCurrent)
            {
                var mcontext = MessageContext.GetCurrent();

                if (mcontext.Items.ContainsKey(Headers.IdempotentSagaResults))
                    mcontext.Items.Remove(Headers.IdempotentSagaResults);
            }
        }

        /// <summary>
        /// Called when [handling error].
        /// </summary>
        /// <param name="exception">The exception.</param>
        private void OnHandlingError(Exception exception)
        {
            RemoveIdempotencyContext();
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
            var id = messageContext.RebusTransportMessageId;
            var serializer = backbone.SerializeMessages;

            log.Debug("Established idempotent saga context for: {0} (Handler: {1})", id, idempotentSagaData.GetType());

            var message = new Message()
            {
                Headers = messageContext.Headers,
                Messages = new object[] { messageContext.CurrentMessage }
            };
            var serializedMessage = serializer.Serialize(message);

            var executionResults = new IdempotentSagaResults(id, serializedMessage, serializer.GetType());
            messageContext.Items[Headers.IdempotentSagaResults] = executionResults;
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
                        log.Debug("Intercepting message {0} to [{1}] as an idempotent side-effect.", 
                            published ? "publication" : "transmission", destinations.Aggregate((cur, next) => cur + ", " + next));

                        var serializer = backbone.SerializeMessages;
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
                mcontext.SkipHandler(handler.GetType());
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

            var mcontext = MessageContext.GetCurrent();
            var icontext = mcontext.Items[Headers.IdempotentSagaResults] as IdempotentSagaResults;

            if (icontext != null)
            {
                log.Debug("Saving results for idempotent invocation of message: {0} (Handler: {1})", 
                    mcontext.RebusTransportMessageId, handler.GetType());

                // Store results of current message handling into our idempotency store.
                idempotentSagaData.ExecutionResults.Add(icontext);
            }

            RemoveIdempotencyContext();
        }

        /// <summary>
        /// Re-sends stored side effects from an already handled message.
        /// </summary>
        /// <param name="handlingData">The handling data.</param>
        /// <param name="bus">The bus.</param>
        /// <param name="messageContext">The message context.</param>
        private void ResendStoredSideEffects(IBus bus, IdempotentSagaResults handlingData, IMessageContext messageContext)
        {
            log.Info("Replaying {0} side-effects relating to a previous idempotent handling of message: {1}.", 
                handlingData.SideEffects.Count, messageContext.RebusTransportMessageId);

            foreach (var item in handlingData.SideEffects)
            {
                var toSend = new TransportMessageToSend()
                {
                    Headers = item.Headers,
                    Body = item.Message
                };

                foreach (var destination in item.Destinations)
                {
                    backbone.SendMessages.Send(destination, toSend, TransactionContext.Current);
                }
            }
        }
    }
}
