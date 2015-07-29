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
        /// used implementation of <see cref="IDetermineMessageOwnership"/>.
        /// </summary>
        void Send<TCommand>(TCommand message);

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
        /// specified by the currently used implementation of <see cref="IDetermineMessageOwnership"/>.
        /// </summary>
        void Subscribe<TEvent>();

        /// <summary>
        /// Sends an unsubscription request for <typeparamref name="TEvent"/> to the destination as
        /// specified by the currently used implementation of <see cref="IDetermineMessageOwnership"/>.
        /// </summary>
        void Unsubscribe<TEvent>();

        /// <summary>
        /// Publishes the specified event message to all endpoints that are currently subscribed.
        /// The publisher should have some kind of <see cref="IStoreSubscriptions"/> implementation,
        /// preferably a durable implementation like e.g. <see cref="SqlServerSubscriptionStorage"/>.
        /// </summary>
        void Publish<TEvent>(TEvent message);

        /// <summary>
        /// Sends the message to the timeout manager, which will send it back after the specified
        /// time span has elapsed. Note that you must have a running timeout manager for this to
        /// work.
        /// </summary>
        void Defer(TimeSpan delay, object message);

        /// <summary>
        /// Attaches to the specified message a header with the given key and value. The header will
        /// be associated with the message, and will be supplied when the message is sent - even if
        /// it is sent multiple times.
        /// </summary>
        void AttachHeader(object message, string key, string value);

        /// <summary>
        /// Gain access to more advanced and less commonly used features of the bus
        /// </summary>
        IAdvancedBus Advanced { get; }
    }
}