using System;
using Rebus.Bus;
using Rebus.Persistence.SqlServer;

namespace Rebus
{
    /// <summary>
    /// This is the main API of Rebus. Most application code should not depend on
    /// any other operation of <see cref="RebusBus"/>.
    /// </summary>
    public interface IBus : IDisposable
    {
        /// <summary>
        /// Sends the specified message to the destination as specified by the currently
        /// used implementation of <see cref="IDetermineDestination"/>.
        /// </summary>
        void Send<TCommand>(TCommand message);

        /// <summary>
        /// Sends the specified message to the specified destination.
        /// </summary>
        void Send<TCommand>(string endpoint, TCommand message);

        /// <summary>
        /// Sends the specified message to the bus' own input queue.
        /// </summary>
        void SendLocal<TCommand>(TCommand message);

        /// <summary>
        /// Sends a reply back to the sender of the message currently being handled. Can only
        /// be called when a <see cref="MessageContext"/> has been established, which happens
        /// during the handling of an incoming message.
        /// </summary>
        void Reply<TResponse>(TResponse message);

        /// <summary>
        /// Sends a subscription request for <typeparamref name="TEvent"/> to the destination as
        /// specified by the currently used implementation of <see cref="IDetermineDestination"/>.
        /// </summary>
        void Subscribe<TEvent>();

        /// <summary>
        /// Sends a subscription request for <typeparamref name="TEvent"/> to the specified 
        /// destination.
        /// </summary>
        void Subscribe<TEvent>(string publisherInputQueue);

        /// <summary>
        /// Publishes the specified event message to all endpoints that are currently subscribed.
        /// The publisher should have some kind of <see cref="IStoreSubscriptions"/> implementation,
        /// preferably a durable implementation like e.g. <see cref="SqlServerSubscriptionStorage"/>.
        /// </summary>
        void Publish<TEvent>(TEvent message);
    }
}