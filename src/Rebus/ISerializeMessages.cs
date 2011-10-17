using Rebus.Messages;

namespace Rebus
{
    /// <summary>
    /// Implement this to specify how messages are represented as strings.
    /// </summary>
    public interface ISerializeMessages
    {
        TransportMessage Serialize(Message message);
        Message Deserialize(TransportMessage transportMessage);
    }
}