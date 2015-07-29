using System;

namespace Rebus
{
    /// <summary>
    /// Groups Rebus' operations for manually routing messages.
    /// </summary>
    public interface IRebusRouting
    {
        /// <summary>
        /// Sends the specified message to the specified destination.
        /// </summary>
        void Send<TCommand>(string destinationEndpoint, TCommand message);

        /// <summary>
        /// Sends a subscription request for <typeparamref name="TEvent"/> to the specified 
        /// destination.
        /// </summary>
        void Subscribe<TEvent>(string publisherInputQueue);

        /// <summary>
        /// Sends an unsubscription request for <typeparamref name="TEvent"/> to the specified 
        /// destination
        /// </summary>
        void Unsubscribe<TEvent>(string publisherInputQueue);

        /// <summary>
        /// Sends a subscription request for the specified event type to the destination as
        /// specified by the currently used implementation of <see cref="IDetermineMessageOwnership"/>.
        /// </summary>
        void Subscribe(Type eventType);

        /// <summary>
        /// Sends a subscription request for the specified event type to the specified destination
        /// </summary>
        void Subscribe(Type eventType, string publisherInputQueue);

        /// <summary>
        /// Sends an unsubscription request for the specified event type to the destination as
        /// specified by the currently used implementation of <see cref="IDetermineMessageOwnership"/>.
        /// </summary>
        void Unsubscribe(Type eventType);

        /// <summary>
        /// Sends an unsubscription request for the specified event type to the specified destination
        /// </summary>
        void Unsubscribe(Type eventType, string publisherInputQueue);

        /// <summary>
        /// Sends the message currently being handled to the specified endpoint, preserving all
        /// of the transport level headers.
        /// </summary>
        void ForwardCurrentMessage(string destinationEndpoint);
    }
}