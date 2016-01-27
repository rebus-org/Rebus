using System;
using System.IO;
using System.Threading.Tasks;
using Rebus.Extensions;
using Rebus.Messages;
using Rebus.Serialization;
using Wire;
#pragma warning disable 1998

namespace Rebus.Wire
{
    /// <summary>
    /// Rebus serializer that uses the binary Wire serializer to provide a robust POCO serialization that supports everything that you would expect from a modern serializer
    /// </summary>
    public class WireSerializer : ISerializer
    {
        /// <summary>
        /// Mime type for Wire
        /// </summary>
        public const string WireContentType = "application/wire";
        readonly Serializer _serializer = new Serializer();

        /// <summary>
        /// Serializes the given <see cref="Message"/> into a <see cref="TransportMessage"/> using the Wire format,
        /// adding a <see cref="Headers.ContentType"/> header with the value of <see cref="WireContentType"/>
        /// </summary>
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

        /// <summary>
        /// Deserializes the given <see cref="TransportMessage"/> back into a <see cref="Message"/>. Expects a
        /// <see cref="Headers.ContentType"/> header with a value of <see cref="WireContentType"/>, otherwise
        /// it will not attempt to deserialize the message.
        /// </summary>
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
