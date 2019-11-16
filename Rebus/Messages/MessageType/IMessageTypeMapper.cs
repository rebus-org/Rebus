using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rebus.Messages.MessageType
{
    /// <summary>
    /// Defines the rules to header type of message
    /// </summary>
    public interface IMessageTypeMapper
    {
        /// <summary>
        /// Returns the header type name based on type of message
        /// </summary>
        string GetMessageType(Type messageType);

        Type GetTypeFromMessage(string messageType);

        bool UseTypeNameHandling { get; }
    }
}
