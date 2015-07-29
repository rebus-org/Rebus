using System;
using System.Collections.Generic;
using System.Transactions;
using Rebus.Extensions;
using Rebus.Messages;

namespace Rebus.Bus
{
    /// <summary>
    /// Implements the explicitly routed messages API by using your ordinary <see cref="RebusBus"/>
    /// </summary>
    public class RebusRouting : IRebusRouting
    {
        readonly RebusBus rebusBus;

        /// <summary>
        /// Constructs the routing API with the specified <see cref="RebusBus"/>
        /// </summary>
        public RebusRouting(RebusBus rebusBus)
        {
            this.rebusBus = rebusBus;
        }

        /// <summary>
        /// Sends the specified message to the specified destination.
        /// </summary>
        public void Send<TCommand>(string destinationEndpoint, TCommand message)
        {
            rebusBus.PossiblyAttachSagaIdToRequest(message);

            rebusBus.InternalSend(new List<string> { destinationEndpoint }, new List<object> { message });
        }

        /// <summary>
        /// Sends a subscription request for <typeparamref name="TEvent"/> to the specified 
        /// destination.
        /// </summary>
        public void Subscribe<TEvent>(string publisherInputQueue)
        {
            rebusBus.InternalSubscribe(publisherInputQueue, typeof(TEvent));
        }

        /// <summary>
        /// Sends an unsubscription request for <typeparamref name="TEvent"/> to the specified 
        /// destination
        /// </summary>
        public void Unsubscribe<TEvent>(string publisherInputQueue)
        {
            rebusBus.InternalUnsubscribe(publisherInputQueue, typeof(TEvent));
        }

        /// <summary>
        /// Sends a subscription request for the specified event type to the destination as
        /// specified by the currently used implementation of <see cref="IDetermineMessageOwnership"/>.
        /// </summary>
        public void Subscribe(Type eventType)
        {
            rebusBus.SendSubscriptionMessage(eventType, SubscribeAction.Subscribe);
        }

        /// <summary>
        /// Sends a subscription request for the specified event type to the specified destination
        /// </summary>
        public void Subscribe(Type eventType, string publisherInputQueue)
        {
            rebusBus.SendSubscriptionMessage(eventType, publisherInputQueue, SubscribeAction.Subscribe);
        }

        /// <summary>
        /// Sends an unsubscription request for the specified event type to the destination as
        /// specified by the currently used implementation of <see cref="IDetermineMessageOwnership"/>.
        /// </summary>
        public void Unsubscribe(Type eventType)
        {
            rebusBus.SendSubscriptionMessage(eventType, SubscribeAction.Unsubscribe);
        }

        /// <summary>
        /// Sends an unsubscription request for the specified event type to the specified destination
        /// </summary>
        public void Unsubscribe(Type eventType, string publisherInputQueue)
        {
            rebusBus.SendSubscriptionMessage(eventType, publisherInputQueue, SubscribeAction.Unsubscribe);
        }

        /// <summary>
        /// Sends the message currently being handled to the specified endpoint, preserving all
        /// of the transport level headers.
        /// </summary>
        public void ForwardCurrentMessage(string destinationEndpoint)
        {
            using (var transactionContext = ManagedTransactionContext.Get())
            {
                var messageContext = MessageContext.GetCurrent();

                var currentMessage = messageContext.CurrentMessage;
                var headers = messageContext.Headers.Clone();

                var message = new Message
                {
                    Headers = headers,
                    Messages = new[] {currentMessage}
                };

                rebusBus.InternalSend(new List<string> { destinationEndpoint }, message, transactionContext.Context);
            }
        }
    }
}
