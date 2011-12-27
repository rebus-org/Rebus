using Rebus.Messages;

namespace Rebus
{
    /// <summary>
    /// Implement this to specify how messages are represented as strings.
    /// </summary>
    public interface ISerializeMessages
    {
        TransportMessageToSend Serialize(Message message);
        Message Deserialize(ReceivedTransportMessage transportMessage);
    }
}