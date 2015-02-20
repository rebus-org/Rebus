using System.Threading.Tasks;
using Rebus2.Messages;

namespace Rebus2.Serialization
{
    public interface ISerializer
    {
        Task<TransportMessage> Serialize(Message message);
        Task<Message> Deserialize(TransportMessage transportMessage);
    }
}