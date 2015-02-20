using System;
using System.Collections.Generic;

namespace Rebus2.Routing
{
    public class SimpleTypeBasedRouter : IRouter
    {
        readonly Dictionary<Type, string> _addresses = new Dictionary<Type, string>();

        public SimpleTypeBasedRouter Map<TMessage>(string destinationAndOwnerAddress)
        {
            _addresses[typeof (TMessage)] = destinationAndOwnerAddress;
            return this;
        }

        public string GetDestinationAddress(object message)
        {
            return GetAddress(message);
        }

        public string GetOwnerAddress(object message)
        {
            return GetAddress(message);
        }

        string GetAddress(object message)
        {
            if (message == null) throw new ArgumentNullException("message");
            var messageType = message.GetType();
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