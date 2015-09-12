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
    }
}