using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rebus.Messages.MessageType
{
    /// <summary>
    /// Mapper for message type
    /// </summary>
    public interface IMessageTypeMapper
    {
        /// <summary>
        /// Returns the header type name based on type of message
        /// </summary>
        string GetMessageType(Type messageType);

        /// <summary>
        /// Returns the type based on header type message
        /// </summary>
        Type GetTypeFromMessage(string messageType);

        /// <summary>
        /// Returns true if serializer can use assembly-qualified to message serialization and deserialization
        /// </summary>
        bool UseTypeNameHandling { get; }
    }
}
