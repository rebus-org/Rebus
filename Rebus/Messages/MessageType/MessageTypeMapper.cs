using System;
using System.Collections.Generic;
using System.Linq;
using Rebus.Exceptions;
using Rebus.Extensions;

namespace Rebus.Messages.MessageType
{
    /// <summary>
    /// MessageTypeMapper permit to configure mapping of class to relative 
    /// header type used in serialization and deserialization
    /// </summary>
    public class MessageTypeMapper : IMessageTypeMapper
    {
        readonly HashSet<(Type Type, string Name)> mappedMessageTypes = new HashSet<(Type Type, string Name)>();

        /// <summary>
        /// Map class to custom name
        /// </summary>
        /// <typeparam name="T">message type</typeparam>
        /// <param name="name">custom name</param>
        /// <returns></returns>
        public MessageTypeMapper Map<T>(string name)
        {
            return this.Map(typeof(T), name);
        }

        /// <summary>
        /// Map class to custom name
        /// </summary>
        /// <param name="type">message type</param>
        /// <param name="name">custom name</param>
        /// <returns></returns>
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

        /// <summary>
        /// Returns the type based on header type message
        /// </summary>
        public Type GetTypeFromMessage(string messageType)
        {
            var tuple = mappedMessageTypes.FirstOrDefault(t => t.Name == messageType);
            if (tuple.Type == null)
            {
                throw new FormatException($"Could not get .NET type named '{messageType}'");
            }
            return tuple.Type;
        }


        /// <summary>
        /// MessageTypeMapper not use assembly name for mapping
        /// </summary>
        public bool UseTypeNameHandling => false;
    }
}