using System;
using Rebus.Extensions;

namespace Rebus.Messages.MessageType
{
    /// <summary>
    /// Default convention to name message type after their "short assembly-qualified type names", which is
    /// an assembly- and namespace-qualified type name without assembly version and public key token info.
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