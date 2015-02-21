using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Rebus2.Messages;

namespace Rebus2.Routing.TypeBased
{
    public class SimpleTypeBasedRouter : IRouter
    {
        readonly Dictionary<string, string> _topicAddresses = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);
        readonly Dictionary<Type, string> _messageTypeAddresses = new Dictionary<Type, string>();

        public SimpleTypeBasedRouter Map<TMessage>(string destinationAddress)
        {
            _messageTypeAddresses[typeof (TMessage)] = destinationAddress;
            return this;
        }

        public SimpleTypeBasedRouter Map(string topic, string ownerAddress)
        {
            _topicAddresses[topic] = ownerAddress;
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

            string ownerAddress;

            if (!_topicAddresses.TryGetValue(topic, out ownerAddress))
            {
                throw new ArgumentException(string.Format("Cannot get owner of topic '{0}' because it has not been mapped!", topic));
            }
            return ownerAddress;
        }

        public async Task<IEnumerable<string>> GetSubscriberAddresses(string topic)
        {
            return Enumerable.Empty<string>();
        }
    }
}