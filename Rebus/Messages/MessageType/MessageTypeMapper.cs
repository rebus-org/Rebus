using System;
using System.Collections.Generic;
using System.Linq;
using Rebus.Exceptions;
using Rebus.Extensions;

namespace Rebus.Messages.MessageType
{
    /// <summary>
    /// Default convention to name message type after their "short assembly-qualified type names", which is
    /// an assembly- and namespace-qualified type name without assembly version and public key token info.
    /// </summary>
    public class MessageTypeMapper : IMessageTypeMapper
    {
        readonly HashSet<(Type Type, string Name)> mappedMessageTypes = new HashSet<(Type Type, string Name)>();

        public MessageTypeMapper Map<T>(string name)
        {
            return this.Map(typeof(T), name);
        }

        public MessageTypeMapper Map(Type type, string name)
        {
            if (mappedMessageTypes.Any(t => t.Name == name))
            {
                throw new RebusConfigurationException($"Message type '{name}' must be unique when use {nameof(MessageTypeMapper)}");
            }

            mappedMessageTypes.Add((Type: type, Name: name));
            return this;
        }

        /// <summary>
        /// Returns the header type name based on type of message
        /// </summary>
        public string GetMessageType(Type messageType)
        {
            var tuple = mappedMessageTypes.FirstOrDefault(t => t.Type == messageType);
            if (String.IsNullOrEmpty(tuple.Name))
            {
                throw new RebusConfigurationException($"Message type '{messageType.Name}' must be registred when use {nameof(MessageTypeMapper)}");
            }
            return tuple.Name;
        }

        public Type GetTypeFromMessage(string messageType)
        {
            var tuple = mappedMessageTypes.FirstOrDefault(t => t.Name == messageType);
            if (tuple.Type == null)
            {
                throw new FormatException($"Could not get .NET type named '{messageType}'");
            }
            return tuple.Type;            
        }

        public bool UseTypeNameHandling => false;
    }
}