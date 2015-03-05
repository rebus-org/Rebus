using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Rebus.Messages;

namespace Rebus.Routing.TypeBased
{
    public class TypeBasedRouter : IRouter
    {
        readonly Dictionary<Type, string> _messageTypeAddresses = new Dictionary<Type, string>();

        public TypeBasedRouter Map<TMessage>(string destinationAddress)
        {
            _messageTypeAddresses[typeof (TMessage)] = destinationAddress;
            return this;
        }

        public async Task<string> GetDestinationAddress(Message message)
        {
            if (message == null) throw new ArgumentNullException("message");
            if (message.Body == null) throw new ArgumentException("message.Body cannot be null when using the simple type-based router");

            var messageType = message.Body.GetType();
            string destinationAddress;
            
            if (!_messageTypeAddresses.TryGetValue(messageType, out destinationAddress))
            {
                throw new ArgumentException(
                    string.Format("Cannot get destination for message of type {0} because it has not been mapped!", messageType));
            }
            
            return destinationAddress;
        }

        public async Task<string> GetOwnerAddress(string topic)
        {
            if (topic == null) throw new ArgumentNullException("topic");

            throw new NotImplementedException("don't know what to do here yet");
        }
    }
}