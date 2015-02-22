using System.Threading.Tasks;
using Rebus2.Messages;

namespace Rebus2.Routing
{
    public interface IRouter
    {
        /// <summary>
        /// Called when sending messages
        /// </summary>
        Task<string> GetDestinationAddress(Message message);

        /// <summary>
        /// Called when subscribing to messages
        /// </summary>
        Task<string> GetOwnerAddress(string topic);
    }
}