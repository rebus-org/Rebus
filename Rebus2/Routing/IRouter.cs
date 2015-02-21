using System.Collections.Generic;
using Rebus2.Messages;

namespace Rebus2.Routing
{
    public interface IRouter
    {
        /// <summary>
        /// Called when sending messages
        /// </summary>
        string GetDestinationAddress(Message message);

        /// <summary>
        /// Called when subscribing to messages
        /// </summary>
        string GetOwnerAddress(Message message);

        /// <summary>
        /// Gets the subscriber addresses for the given topic
        /// </summary>
        IEnumerable<string> GetSubscribers(string topic);
    }
}