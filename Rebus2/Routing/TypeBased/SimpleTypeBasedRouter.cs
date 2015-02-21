using System;
using System.Collections.Generic;
using System.Linq;
using Rebus2.Messages;

namespace Rebus2.Routing.TypeBased
{
    public class SimpleTypeBasedRouter : IRouter
    {
        readonly Dictionary<Type, string> _addresses = new Dictionary<Type, string>();

        public SimpleTypeBasedRouter Map<TMessage>(string destinationAndOwnerAddress)
        {
            _addresses[typeof (TMessage)] = destinationAndOwnerAddress;
            return this;
        }

        public string GetDestinationAddress(Message message)
        {
            return GetAddress(message);
        }

        public string GetOwnerAddress(Message message)
        {
            return GetAddress(message);
        }

        public IEnumerable<string> GetSubscribers(string topic)
        {
            return Enumerable.Empty<string>();
        }

        string GetAddress(Message message)
        {
            if (message == null) throw new ArgumentNullException("message");
            if (message.Body == null) throw new ArgumentException("message.Body cannot be null when using the simple type-based router");

            var messageType = message.Body.GetType();
            string destinationAddress;
            
            if (!_addresses.TryGetValue(messageType, out destinationAddress))
            {
                throw new InvalidOperationException(
                    string.Format("Cannot route message of type {0} because it has not been mapped!", messageType));
            }
            
            return destinationAddress;
        }
    }
}