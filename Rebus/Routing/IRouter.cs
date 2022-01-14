using System.Threading.Tasks;
using Rebus.Messages;

namespace Rebus.Routing;

/// <summary>
/// Abstraction of the routing logic. Should be capable of returning a destination address for a message
/// and an owner address for a topic.
/// </summary>
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