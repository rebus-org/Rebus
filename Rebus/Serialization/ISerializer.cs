using System.Threading.Tasks;
using Rebus.Messages;

namespace Rebus.Serialization;

/// <summary>
/// Message serializer that should capable of safely roundtripping .NET message body objects to some serialized form and back
/// </summary>
public interface ISerializer
{
    /// <summary>
    /// Serializes the given <see cref="Message"/> into a <see cref="TransportMessage"/>
    /// </summary>
    Task<TransportMessage> Serialize(Message message);

    /// <summary>
    /// Deserializes the given <see cref="TransportMessage"/> back into a <see cref="Message"/>
    /// </summary>
    Task<Message> Deserialize(TransportMessage transportMessage);
}