using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Rebus.Logging;
using Rebus.Messages;
#pragma warning disable 1998

namespace Rebus.Routing.TypeBased;

/// <summary>
/// Routing logic that maps types to owning endpoints.
/// </summary>
public class TypeBasedRouter : IRouter
{
    readonly Dictionary<Type, string> _messageTypeAddresses = new Dictionary<Type, string>();
    readonly ILog _log;

    string _fallbackAddress;

    /// <summary>
    /// Constructs the router
    /// </summary>
    public TypeBasedRouter(IRebusLoggerFactory rebusLoggerFactory)
    {
        if (rebusLoggerFactory == null) throw new ArgumentNullException(nameof(rebusLoggerFactory));
        _log = rebusLoggerFactory.GetLogger<TypeBasedRouter>();
    }

    /// <summary>
    /// Maps <paramref name="destinationAddress"/> as the owner of all message types found in the same assembly as <typeparamref name="TMessage"/>
    /// </summary>
    public TypeBasedRouter MapAssemblyOf<TMessage>(string destinationAddress)
    {
        MapAssemblyOf(typeof (TMessage), destinationAddress);
        return this;
    }

    /// <summary>
    /// Maps <paramref name="destinationAddress"/> as the owner of all message types found in the same assembly as <paramref name="messageType"/>
    /// </summary>
    public TypeBasedRouter MapAssemblyOf(Type messageType, string destinationAddress)
    {
        foreach (var typeToMap in messageType.GetTypeInfo().Assembly.GetTypes().Where(t => t.IsClass))
        {
            SaveMapping(typeToMap, destinationAddress);
        }
        return this;
    }

    /// <summary>
    /// Maps <paramref name="destinationAddress"/> as the owner of all message types found in the same assembly as <typeparamref name="TDerivedFrom"/>
    /// and derived from <typeparamref name="TDerivedFrom"/>
    /// </summary>
    public TypeBasedRouter MapAssemblyDerivedFrom<TDerivedFrom>(string destinationAddress)
    {
        MapAssemblyDerivedFrom(typeof(TDerivedFrom), destinationAddress);
        return this;
    }

    /// <summary>
    /// Maps <paramref name="destinationAddress"/> as the owner of all message types found in the same assembly as <paramref name="derivedFrom"/>
    /// and optionally derived from <paramref name="derivedFrom"/>
    /// </summary>
    public TypeBasedRouter MapAssemblyDerivedFrom(Type derivedFrom, string destinationAddress)
    {
        foreach (var typeToMap in derivedFrom.GetTypeInfo().Assembly.GetTypes().Where(t => t.IsClass))
        {
            if (derivedFrom == null || typeToMap != derivedFrom && derivedFrom.IsAssignableFrom(typeToMap))
            {
                SaveMapping(typeToMap, destinationAddress);
            }
        }
        return this;
    }

    /// <summary>
    /// Maps <paramref name="destinationAddress"/> as the owner of all message types found in the same assembly as <typeparamref name="TMessage"/> under
    /// the namespace that type lives under. So all types within the same namespace will get mapped to that destination address, but not types under
    /// other namespaces. This allows you to separate messages for specific queues by namespace and register them all in one go.
    /// </summary>
    public TypeBasedRouter MapAssemblyNamespaceOf<TMessage>(string destinationAddress)
    {
        MapAssemblyNamespaceOf(typeof(TMessage), destinationAddress);
        return this;
    }

    /// <summary>
    /// Maps <paramref name="destinationAddress"/> as the owner of all message types found in the same assembly as <paramref name="messageType"/> under
    /// the namespace that type lives under. So all types within the same namespace will get mapped to that destination address, but not types under
    /// other namespaces. This allows you to separate messages for specific queues by namespace and register them all in one go.
    /// </summary>
    public TypeBasedRouter MapAssemblyNamespaceOf(Type messageType, string destinationAddress)
    {
        foreach (var typeToMap in messageType.GetTypeInfo().Assembly.GetTypes().Where(t => t.IsClass && t.Namespace != null && t.Namespace.StartsWith(messageType.Namespace ?? string.Empty)))
        {
            SaveMapping(typeToMap, destinationAddress);
        }
        return this;
    }

    /// <summary>
    /// Maps <paramref name="destinationAddress"/> as the owner of all message types found in the same assembly as <typeparamref name="TMessage"/> under
    /// the namespace that type lives under and derived from <typeparamref name="TDerivedFrom"/>. So all types within the same namespace will
    /// get mapped to that destination address, but not types under other namespaces. This allows you to separate messages for specific queues by
    /// namespace and register them all in one go.
    /// </summary>
    public TypeBasedRouter MapAssemblyNamespaceOfDerivedFrom<TMessage, TDerivedFrom>(string destinationAddress)
    {
        MapAssemblyNamespaceOfDerivedFrom(typeof(TMessage), typeof(TDerivedFrom), destinationAddress);
        return this;
    }

    /// <summary>
    /// Maps <paramref name="destinationAddress"/> as the owner of all message types found in the same assembly as <paramref name="messageType"/> under
    /// the namespace that type lives under and derived from <paramref name="derivedFrom"/>. So all types within the same namespace will
    /// get mapped to that destination address, but not types under other namespaces. This allows you to separate messages for specific queues by
    /// namespace and register them all in one go.
    /// </summary>
    public TypeBasedRouter MapAssemblyNamespaceOfDerivedFrom(Type messageType, Type derivedFrom, string destinationAddress)
    {
        foreach (var typeToMap in messageType.GetTypeInfo().Assembly.GetTypes().Where(t => t.IsClass && t.Namespace != null && t.Namespace.StartsWith(messageType.Namespace ?? string.Empty)))
        {
            if (derivedFrom == null || typeToMap != derivedFrom && derivedFrom.IsAssignableFrom(typeToMap))
            {
                SaveMapping(typeToMap, destinationAddress);
            }
        }
        return this;
    }

    /// <summary>
    /// Maps <paramref name="destinationAddress"/> as the owner of the <typeparamref name="TMessage"/> message type
    /// </summary>
    public TypeBasedRouter Map<TMessage>(string destinationAddress)
    {
        SaveMapping(typeof(TMessage), destinationAddress);
        return this;
    }

    /// <summary>
    /// Configures <paramref name="destinationAddress"/> as a fallback which will be returned when trying to get a destination for an unmapped type
    /// </summary>
    public TypeBasedRouter MapFallback(string destinationAddress)
    {
        if (destinationAddress == null) throw new ArgumentNullException(nameof(destinationAddress));

        if (_fallbackAddress != null)
        {
            _log.Warn("Existing fallback mapping -> {queueName} changed to -> {newQueueName}", _fallbackAddress, destinationAddress);
        }

        _fallbackAddress = destinationAddress;

        return this;
    }

    /// <summary>
    /// Maps <paramref name="destinationAddress"/> as the owner of the <paramref name="messageType"/> message type
    /// </summary>
    public TypeBasedRouter Map(Type messageType, string destinationAddress)
    {
        SaveMapping(messageType, destinationAddress);
        return this;
    }

    void SaveMapping(Type messageType, string destinationAddress)
    {
        if (messageType == null) throw new ArgumentNullException(nameof(messageType));
        if (destinationAddress == null) throw new ArgumentNullException(nameof(destinationAddress));

        if (_messageTypeAddresses.ContainsKey(messageType) &&
            _messageTypeAddresses[messageType] != destinationAddress)
        {
            _log.Warn("Existing endpoint mapping {messageType} -> {queueName} changed to -> {newQueueName}",
                messageType, _messageTypeAddresses[messageType], destinationAddress);
        }
        else
        {
            _log.Info("Mapped {messageType} -> {queueName}", messageType, destinationAddress);
        }

        _messageTypeAddresses[messageType] = destinationAddress;
    }

    /// <summary>
    /// Gets the destination address for the given message
    /// </summary>
    public async Task<string> GetDestinationAddress(Message message)
    {
        if (message == null) throw new ArgumentNullException(nameof(message));
        if (message.Body == null) throw new ArgumentException("message.Body cannot be null when using the type-based router");

        return GetDestinationAddressForMessageType(message.Body.GetType());
    }

    /// <summary>
    /// Looks up the owner of the topic which is assumed to be an assembly-qualified name of an available .NET type
    /// </summary>
    public async Task<string> GetOwnerAddress(string topic)
    {
        if (topic == null) throw new ArgumentNullException(nameof(topic));

        var messageType = GetMessageTypeFromTopic(topic);

        return GetDestinationAddressForMessageType(messageType);
    }

    static Type GetMessageTypeFromTopic(string topic)
    {
        try
        {
            return Type.GetType(topic, true, true);
        }
        catch (Exception exception)
        {
            throw new ArgumentException(
                $"The topic '{topic}' could not be mapped to a message type! When using the type-based router, only topics based on proper, accessible .NET types can be used!", exception);
        }
    }

    string GetDestinationAddressForMessageType(Type messageType)
    {
        if (!_messageTypeAddresses.TryGetValue(messageType, out var destinationAddress))
        {
            if (_fallbackAddress != null) return _fallbackAddress;

            throw new ArgumentException(
                $@"Cannot get destination for message of type {messageType} because it has not been mapped! 

You need to ensure that all message types that you intend to bus.Send or bus.Subscribe to are mapped to an endpoint - it can be done by calling .Map<SomeMessage>(someEndpoint) or .MapAssemblyOf<SomeMessage>(someEndpoint) in the routing configuration.");
        }

        return destinationAddress;
    }
}