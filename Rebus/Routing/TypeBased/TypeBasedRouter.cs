using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Rebus.Logging;
using Rebus.Messages;
#pragma warning disable 1998

namespace Rebus.Routing.TypeBased
{
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
            _log = rebusLoggerFactory.GetCurrentClassLogger();
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
            foreach (var typeToMap in messageType.GetTypeInfo().Assembly.GetTypes())
            {
                SaveMapping(typeToMap, destinationAddress);
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
                _log.Warn("Existing fallback mapping -> {0} overridden by -> {1}", _fallbackAddress, destinationAddress);
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
                _log.Warn("Existing endpoint mapping {0} -> {1} overridden by {0} -> {2}",
                    messageType, _messageTypeAddresses[messageType], destinationAddress);
            }
            else
            {
                _log.Info("Mapped {0} -> {1}", messageType, destinationAddress);
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
            string destinationAddress;

            if (!_messageTypeAddresses.TryGetValue(messageType, out destinationAddress))
            {
                if (_fallbackAddress != null) return _fallbackAddress;

                throw new ArgumentException(
                    $@"Cannot get destination for message of type {messageType} because it has not been mapped! 

You need to ensure that all message types that you intend to bus.Send or bus.Subscribe to are mapped to an endpoint - it can be done by calling .Map<SomeMessage>(someEndpoint) or .MapAssemblyOf<SomeMessage>(someEndpoint) in the routing configuration.");
            }

            return destinationAddress;
        }
    }
}