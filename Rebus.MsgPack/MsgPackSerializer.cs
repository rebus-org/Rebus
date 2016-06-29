using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using MsgPack;
using Rebus.Extensions;
using Rebus.Messages;
using Rebus.Serialization;
#pragma warning disable 1998

namespace Rebus.MsgPack
{
    /// <summary>
    /// Implementation of <see cref="ISerializer"/> that uses MsgPack to serialize messages
    /// </summary>
    public class MsgPackSerializer : ISerializer
    {
        const string MsgPackContentType = "application/x-msgpack";
        readonly ObjectPacker _packer = new ObjectPacker();

        /// <summary>
        /// Serializes the given logical message to a transport message using MsgPack. Adds the
        /// <see cref="Headers.ContentType"/> with the <code>application/x-msgpack</code> value
        /// and sets the <see cref="Headers.Type"/> to the short assembly-qualified .NET type name of the message body
        /// </summary>
        public async Task<TransportMessage> Serialize(Message message)
        {
            var headers = message.Headers.Clone();
            var body = message.Body;

            headers[Headers.ContentType] = MsgPackContentType;
            headers[Headers.Type] = body.GetType().GetSimpleAssemblyQualifiedName();

            var bytes = _packer.Pack(body);

            return new TransportMessage(headers, bytes);
        }

        /// <summary>
        /// Deserializes the given transport message to a logical message using MsgPack. Throws if the
        /// required <see cref="Headers.ContentType"/> does not have the required <code>application/x-msgpack</code> value
        /// or of the .NET type of the message cannot be resolved from the <see cref="Headers.Type"/> header (which
        /// should carry the short assembly-qualified .NET type name of the message body)
        /// </summary>
        public async Task<Message> Deserialize(TransportMessage transportMessage)
        {
            var headers = transportMessage.Headers;
            var contentType = headers.GetValue(Headers.ContentType);

            if (contentType != MsgPackContentType)
            {
                throw new FormatException($"Unknown content type: '{contentType}' - must be '{MsgPackContentType}' for the MsgPack serialier to work");
            }

            var messageType = GetMessageType(headers);
            var body = _packer.Unpack(messageType, transportMessage.Body);

            return new Message(headers.Clone(), body);
        }

        static Type GetMessageType(IDictionary<string, string> headers)
        {
            string messageTypeString;
            if (!headers.TryGetValue(Headers.Type, out messageTypeString))
            {
                throw new SerializationException($"Could not find the '{Headers.Type}' header on the incoming message");
            }

            var type = Type.GetType(messageTypeString, false);

            if (type == null)
            {
                var message = $"Could not find .NET type matching '{messageTypeString}' - please be sure that the correct message" +
                    " assembly is available when handling messages";

                throw new SerializationException(message);
            }

            return type;
        }

    }
}
