using System;
using Rebus.Extensions;
using Rebus.Messages.MessageType;

namespace Rebus.Topic
{
    /// <summary>
    /// Default convention to name topics after their "short assembly-qualified type names", which is
    /// an assembly- and namespace-qualified type name without assembly version and public key token info.
    /// </summary>
    public class DefaultTopicNameConvention : ITopicNameConvention
    {
        private IMessageTypeMapper _messageTypeMapper;

        /// <summary>
        /// Default constructor
        /// </summary>
        /// <param name="messageTypeMapper"></param>
        public DefaultTopicNameConvention(IMessageTypeMapper messageTypeMapper)
        {
            _messageTypeMapper = messageTypeMapper;
        }

        /// <summary>
        /// Returns the default topic name based on the "short assembly-qualified type name", which is
        /// an assembly- and namespace-qualified type name without assembly version and public key token info.
        /// </summary>
        public string GetTopic(Type eventType)
        {
            return _messageTypeMapper.GetMessageType(eventType);
        }
    }
}