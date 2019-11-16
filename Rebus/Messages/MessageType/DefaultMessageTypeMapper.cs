using System;
using Rebus.Extensions;

namespace Rebus.Messages.MessageType
{
    /// <summary>
    /// Default Mapper for message type on "short assembly-qualified type names"
    /// </summary>
    public class DefaultMessageTypeMapper : IMessageTypeMapper
    {
        public bool UseTypeNameHandling => true;

        /// <summary>
        /// Returns the header type name based on type of message
        /// </summary>
        public string GetMessageType(Type messageType)
        {
            return messageType.GetSimpleAssemblyQualifiedName();
        }

        /// <summary>
        /// Returns the type based on header type message
        /// </summary>
        public Type GetTypeFromMessage(string messageType)
        {
            try
            {
                return Type.GetType(messageType);
            }
            catch (Exception exception)
            {
                throw new FormatException($"Could not get .NET type named '{messageType}'", exception);
            }
        }
    }
}