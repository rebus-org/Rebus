using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Rebus.Bus.Advanced;
using Rebus.Messages;
using Rebus.Routing;

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
        /// Defers the delivery of the message by attaching a <see cref="Headers.DeferredUntil"/> header to it and delivering it to the configured timeout manager endpoint
        /// (defaults to be ourselves). When the time is right, the deferred message is returned to the address indicated by the <see cref="Headers.ReturnAddress"/> header.
        /// </summary>
        Task Defer(TimeSpan delay, object message, Dictionary<string, string> optionalHeaders = null);

        /// <summary>
        /// Gets the API for advanced features of the bus
        /// </summary>
        IAdvancedApi Advanced { get; }

        /// <summary>
        /// Subscribes to the topic defined by the assembly-qualified name of <typeparamref name="TEvent"/>. 
        /// While this kind of subscription can work universally with the general topic-based routing, it works especially well with type-based routing,
        /// which can be enabled by going 
        /// <code>
        /// Configure.With(...)
        ///     .(...)
        ///     .Routing(r => r.TypeBased()
        ///             .Map&lt;SomeMessage&gt;("someEndpoint")
        ///             .(...))
        /// </code>
        /// in the configuration
        /// </summary>
        Task Subscribe<TEvent>();

        /// <summary>
        /// Subscribes to the topic defined by the assembly-qualified name of <paramref name="eventType"/>. 
        /// While this kind of subscription can work universally with the general topic-based routing, it works especially well with type-based routing,
        /// which can be enabled by going 
        /// <code>
        /// Configure.With(...)
        ///     .(...)
        ///     .Routing(r => r.TypeBased()
        ///             .Map&lt;SomeMessage&gt;("someEndpoint")
        ///             .(...))
        /// </code>
        /// in the configuration
        /// </summary>
        Task Subscribe(Type eventType);

        /// <summary>
        /// Unsubscribes from the topic defined by the assembly-qualified name of <typeparamref name="TEvent"/>
        /// </summary>
        Task Unsubscribe<TEvent>();
        
        /// <summary>
        /// Unsubscribes from the topic defined by the assembly-qualified name of <paramref name="eventType"/>
        /// </summary>
        Task Unsubscribe(Type eventType);

        /// <summary>
        /// Publishes the event message on the topic defined by the assembly-qualified name of the type of the message.
        /// While this kind of pub/sub can work universally with the general topic-based routing, it works especially well with type-based routing,
        /// which can be enabled by going 
        /// <code>
        /// Configure.With(...)
        ///     .(...)
        ///     .Routing(r => r.TypeBased()
        ///             .Map&lt;SomeMessage&gt;("someEndpoint")
        ///             .(...))
        /// </code>
        /// in the configuration
        /// </summary>
        Task Publish(object eventMessage, Dictionary<string, string> optionalHeaders = null);
    }
}