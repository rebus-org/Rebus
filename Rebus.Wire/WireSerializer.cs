using System;
using System.IO;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using Rebus.Extensions;
using Rebus.Messages;
using Rebus.Serialization;
using Wire;

namespace Rebus.Wire
{
    public class WireSerializer : ISerializer
    {
        const string WireContentType = "application/wire";
        readonly Serializer _serializer = new Serializer();

        public async Task<TransportMessage> Serialize(Message message)
        {
            using (var destination = new MemoryStream())
            {
                _serializer.Serialize(message.Body, destination);

                var headers = message.Headers.Clone();

                headers[Headers.ContentType] = WireContentType;

                return new TransportMessage(headers, destination.ToArray());
            }
        }

        public async Task<Message> Deserialize(TransportMessage transportMessage)
        {
            var contentType = transportMessage.Headers.GetValue(Headers.ContentType);

            if (contentType != WireContentType)
            {
                throw new FormatException($"Unknown content type: '{contentType}' - must be '{WireContentType}' for the JSON serialier to work");
            }

            using (var source = new MemoryStream(transportMessage.Body))
            {
                var body = _serializer.Deserialize(source);

                return new Message(transportMessage.Headers.Clone(), body);
            }
        }
    }
}
