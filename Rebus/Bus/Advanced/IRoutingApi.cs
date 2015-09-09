using System.Collections.Generic;
using System.Threading.Tasks;

namespace Rebus.Bus.Advanced
{
    /// <summary>
    /// Provides a raw API for explicitly routing messages to other endpoints
    /// </summary>
    public interface IRoutingApi
    {
        /// <summary>
        /// Explicitly routes the <paramref name="explicitlyRoutedMessage"/> to the destination specified by <paramref name="destinationAddress"/>
        /// </summary>
        Task Send(string destinationAddress, object explicitlyRoutedMessage, Dictionary<string, string> optionalHeaders = null);

        /// <summary>
        /// Forwards the transport message currently being handled to the specified queue, optionally supplying some extra headers
        /// </summary>
        Task Forward(string destinationAddress, Dictionary<string, string> optionalAdditionalHeaders = null);
    }
}