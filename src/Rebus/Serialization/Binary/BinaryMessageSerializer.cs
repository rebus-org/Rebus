using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using Rebus.Messages;
using Rebus.Extensions;

namespace Rebus.Serialization.Binary
{
    public class BinaryMessageSerializer : ISerializeMessages
    {
        public TransportMessageToSend Serialize(Message message)
        {
            using (var memoryStream = new MemoryStream())
            {
                var formatter = new BinaryFormatter();
                formatter.Serialize(memoryStream, message);
                memoryStream.Position = 0;
                
                return new TransportMessageToSend
                           {
                               Label = message.GetLabel(),
                               Headers = message.Headers.Clone(),
                               Body = memoryStream.ToArray(),
                           };
            }
        }

        public Message Deserialize(ReceivedTransportMessage transportMessage)
        {
            using (var memoryStream = new MemoryStream(transportMessage.Body))
            {
                var formatter = new BinaryFormatter();
                var message = (Message) formatter.Deserialize(memoryStream);
                return message;
            }
        }
    }
}