using System.Threading.Tasks;
using Rebus.Messages;

namespace Rebus.Serialization
{
    public interface ISerializer
    {
        Task<TransportMessage> Serialize(Message message);
        Task<Message> Deserialize(TransportMessage transportMessage);
    }
}