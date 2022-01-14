using System;
using System.Collections.Generic;
using Rebus.Messages;
using Rebus.Routing;

namespace Rebus.Bus.Advanced;

/// <summary>
/// Synchronous API for all of the async-native operations of the Rebus <see cref="IBus"/>. May be used in applications 
/// that need to perform bus operations deep within a call hierarchy, or that simply do not contain an appropriate place
/// to await something. Safe to use in applications that insist on running continuations on the initiating thread, like 
/// e.g. ASP.NET and WPF.
/// </summary>
public interface ISyncBus
{
    /// <summary>
    /// Sends the specified message to our own input queue address
    /// </summary>
    void SendLocal(object commandMessage, IDictionary<string, string> optionalHeaders = null);

    /// <summary>
    /// Sends the specified message to a destination that is determined by calling <see cref="IRouter.GetDestinationAddress"/>
    /// </summary>
    void Send(object commandMessage, IDictionary<string, string> optionalHeaders = null);

    /// <summary>
    /// Sends the specified reply message to a destination that is determined by looking up the <see cref="Headers.ReturnAddress"/> header of the message currently being handled.
    /// This method can only be called from within a message handler.
    /// </summary>
    void Reply(object replyMessage, IDictionary<string, string> optionalHeaders = null);

    /// <summary>
    /// Defers into the future the specified message, optionally specifying some headers to attach to the message. Unless the <see cref="Headers.DeferredRecipient"/> is specified
    /// in a header, the endpoint mapping corresponding to the sent message will be set as the return address, which will cause the message to be delivered to that address when the <paramref name="delay"/>
    /// has elapsed.
    /// </summary>
    void Defer(TimeSpan delay, object message, IDictionary<string, string> optionalHeaders = null);

    /// <summary>
    /// Defers into the future the specified message, optionally specifying some headers to attach to the message. Unless the <see cref="Headers.DeferredRecipient"/> is specified
    /// in a header, the bus instance's own input address will be set as the return address, which will cause the message to be delivered to that address when the <paramref name="delay"/>
    /// has elapsed.
    /// </summary>
    void DeferLocal(TimeSpan delay, object message, IDictionary<string, string> optionalHeaders = null);

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
    void Subscribe<TEvent>();

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
    void Subscribe(Type eventType);

    /// <summary>
    /// Unsubscribes from the topic defined by the assembly-qualified name of <typeparamref name="TEvent"/>
    /// </summary>
    void Unsubscribe<TEvent>();

    /// <summary>
    /// Unsubscribes from the topic defined by the assembly-qualified name of <paramref name="eventType"/>
    /// </summary>
    void Unsubscribe(Type eventType);

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
    void Publish(object eventMessage, IDictionary<string, string> optionalHeaders = null);
}