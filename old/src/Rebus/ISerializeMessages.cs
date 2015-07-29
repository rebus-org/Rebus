using Rebus.Messages;

namespace Rebus
{
    /// <summary>
    /// Implement this to specify how messages are represented as strings.
    /// </summary>
    public interface ISerializeMessages
    {
        /// <summary>
        /// Serializes the specified <see cref="Message"/> object into a <see cref="TransportMessageToSend"/>,
        /// which is just a container for a headers dictionary and a byte array for the body.
        /// </summary>
        TransportMessageToSend Serialize(Message message);
        
        /// <summary>
        /// Deserializes the specified <see cref="ReceivedTransportMessage"/>, which is a container for a
        /// headers dictionary, a byte array for the body, and an ID (possibly assigned by the infrastructure),
        /// into a <see cref="Message"/> object.
        /// </summary>
        Message Deserialize(ReceivedTransportMessage transportMessage);
    }
}