using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Rebus.Messages;

namespace Rebus.Routing.TopicBased
{
    /// <summary>
    /// Implementation of <see cref="IRouter"/> that uses string-based topics to do its thing
    /// </summary>
    public class TopicBasedRouter : IRouter
    {
        readonly Dictionary<string, string> _topicAddresses = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);

        /// <summary>
        /// Maps the specified topic to the specified address
        /// </summary>
        public TopicBasedRouter Map(string topic, string ownerAddress)
        {
            _topicAddresses[topic] = ownerAddress;
            return this;
        }

        public async Task<string> GetDestinationAddress(Message message)
        {
            if (message == null) throw new ArgumentNullException("message");

            throw new NotImplementedException("don't know what to do here yet");
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
    }
}