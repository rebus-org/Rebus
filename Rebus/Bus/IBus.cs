using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Rebus.Bus.Advanced;
using Rebus.Messages;
using Rebus.Messages.Control;
using Rebus.Routing;
using Rebus.Subscriptions;

namespace Rebus.Bus
{
    /// <summary>
    /// This is the main bus API
    /// </summary>
    public interface IBus : IDisposable
    {
        /// <summary>
        /// Sends the specified message to our own input queue address
        /// </summary>
        Task SendLocal(object commandMessage, Dictionary<string, string> optionalHeaders = null);

        /// <summary>
        /// Sends the specified message to a destination that is determined by calling <see cref="IRouter.GetDestinationAddress"/>
        /// </summary>
        Task Send(object commandMessage, Dictionary<string, string> optionalHeaders = null);

        /// <summary>
        /// Sends the specified reply message to a destination that is determined by looking up the <see cref="Headers.ReturnAddress"/> header of the message currently being handled.
        /// This method can only be called from within a message handler.
        /// </summary>
        Task Reply(object replyMessage, Dictionary<string, string> optionalHeaders = null);

        /// <summary>
        /// Publishes the specified message to the specified topic. Default behavior is to look up the addresses of those who subscribed to the given topic
        /// by calling <see cref="ISubscriptionStorage.GetSubscriberAddresses"/> but the transport may override this behavior if it has special capabilities.
        /// </summary>
        Task Publish(string topic, object eventMessage, Dictionary<string, string> optionalHeaders = null);

        /// <summary>
        /// Defers the delivery of the message by attaching a <see cref="Headers.DeferredUntil"/> header to it and delivering it to the configured timeout manager endpoint
        /// (defaults to be ourselves). When the time is right, the deferred message is returned to the address indicated by the <see cref="Headers.ReturnAddress"/> header.
        /// </summary>
        Task Defer(TimeSpan delay, object message, Dictionary<string, string> optionalHeaders = null);

        /// <summary>
        /// Subscribes the current endpoint to the given topic. If the <see cref="ISubscriptionStorage"/> is centralized (determined by checking <see cref="ISubscriptionStorage.IsCentralized"/>),
        /// the subscription is registered immediately. If not, the owner of the given topic is checked (by calling <see cref="IRouter.GetOwnerAddress"/>), and a
        /// <see cref="SubscribeRequest"/> is sent to the owning endpoint).
        /// </summary>
        Task Subscribe(string topic);

        /// <summary>
        /// Unsubscribes the current endpoint from the given topic. If the <see cref="ISubscriptionStorage"/> is centralized (determined by checking <see cref="ISubscriptionStorage.IsCentralized"/>),
        /// the subscription is removed immediately. If not, the owner of the given topic is checked (by calling <see cref="IRouter.GetOwnerAddress"/>), and an
        /// <see cref="UnsubscribeRequest"/> is sent to the owning endpoint).
        /// </summary>
        Task Unsubscribe(string topic);

        /// <summary>
        /// Explicitly routes the <see cref="explicitlyRoutedMessage"/> to the destination specified by <see cref="destinationAddress"/>
        /// </summary>
        Task Route(string destinationAddress, object explicitlyRoutedMessage, Dictionary<string, string> optionalHeaders = null);

        /// <summary>
        /// Gets the API for advanced features of the bus
        /// </summary>
        IAdvancedApi Advanced { get; }
    }
}