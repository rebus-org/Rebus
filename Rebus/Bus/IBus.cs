using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Rebus.Bus.Advanced;
using Rebus.Messages;

namespace Rebus.Bus;

/// <summary>
/// This is the main bus API
/// </summary>
public interface IBus : IDisposable
{
    /// <summary>
    /// Sends the specified command message to this instance's own input queue, optionally specifying some headers to attach to the message
    /// </summary>
    Task SendLocal(object commandMessage, IDictionary<string, string> optionalHeaders = null);

    /// <summary>
    /// Sends the specified command message to the address mapped as the owner of the message type, optionally specifying some headers to attach to the message
    /// </summary>
    Task Send(object commandMessage, IDictionary<string, string> optionalHeaders = null);

    /// <summary>
    /// Defers the message delivery into the future, optionally specifying some headers to attach to the message. Unless the <see cref="Headers.DeferredRecipient"/> is specified
    /// in a header, the bus instance's own input queue address will be set as the return address, which will cause the message to be delivered to that address when the <paramref name="delay"/>
    /// has elapsed.
    /// </summary>
    Task DeferLocal(TimeSpan delay, object message, IDictionary<string, string> optionalHeaders = null);

    /// <summary>
    /// Defers the message delivery into the future, optionally specifying some headers to attach to the message. Unless the <see cref="Headers.DeferredRecipient"/> is specified
    /// in a header, the endpoint mapping corresponding to the sent message will be set as the return address, which will cause the message to be delivered to that address when the <paramref name="delay"/>
    /// has elapsed.
    /// </summary>
    Task Defer(TimeSpan delay, object message, IDictionary<string, string> optionalHeaders = null);

    /// <summary>
    /// Replies back to the endpoint specified as return address on the message currently being handled. Throws an <see cref="InvalidOperationException"/> if called outside of a proper message context.
    /// </summary>
    Task Reply(object replyMessage, IDictionary<string, string> optionalHeaders = null);

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
    Task Publish(object eventMessage, IDictionary<string, string> optionalHeaders = null);

    /// <summary>
    /// Gets the API for advanced features of the bus
    /// </summary>
    IAdvancedApi Advanced { get; }
}