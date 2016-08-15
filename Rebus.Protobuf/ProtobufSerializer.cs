using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using ProtoBuf.Meta;
using Rebus.DataBus;
using Rebus.Extensions;
using Rebus.Messages;
using Rebus.Messages.Control;
using Rebus.Serialization;
#pragma warning disable 1998

namespace Rebus.Protobuf
{
    class ProtobufSerializer : ISerializer
    {
        readonly RuntimeTypeModel _runtimeTypeModel;

        public ProtobufSerializer(RuntimeTypeModel runtimeTypeModel)
        {
            if (runtimeTypeModel == null) throw new ArgumentNullException(nameof(runtimeTypeModel));

            _runtimeTypeModel = runtimeTypeModel;

            var subscribeRequestType = _runtimeTypeModel.Add(typeof (SubscribeRequest), true);
            subscribeRequestType.AddField(1, Reflect.Path<SubscribeRequest>(r => r.Topic));
            subscribeRequestType.AddField(2, Reflect.Path<SubscribeRequest>(r => r.SubscriberAddress));

            var unsubscribeRequestType = _runtimeTypeModel.Add(typeof (UnsubscribeRequest), true);
            unsubscribeRequestType.AddField(1, Reflect.Path<UnsubscribeRequest>(r => r.Topic));
            unsubscribeRequestType.AddField(2, Reflect.Path<UnsubscribeRequest>(r => r.SubscriberAddress));

            var dataBusAttachmentType = _runtimeTypeModel.Add(typeof(DataBusAttachment), true);
            dataBusAttachmentType.AddField(1, Reflect.Path<DataBusAttachment>(r => r.Id));
        }

        public ProtobufSerializer()
            : this(RuntimeTypeModel.Default)
        {
        }

        public async Task<TransportMessage> Serialize(Message message)
        {
            using (var destination = new MemoryStream())
            {
                var headers = message.Headers.Clone();

                if (!headers.ContainsKey(Headers.Type))
                {
                    headers[Headers.Type] = message.Body.GetType().GetSimpleAssemblyQualifiedName();
                }

                _runtimeTypeModel.Serialize(destination, message.Body);

                return new TransportMessage(headers, destination.ToArray());
            }
        }

        public async Task<Message> Deserialize(TransportMessage transportMessage)
        {
            using (var source = new MemoryStream(transportMessage.Body))
            {
                var headers = transportMessage.Headers.Clone();

                var body = _runtimeTypeModel.Deserialize(source, null, GetMessageType(transportMessage.Headers));

                return new Message(headers, body);
            }
        }

        static Type GetMessageType(Dictionary<string, string> headers)
        {
            string messageTypeString;
            if (!headers.TryGetValue(Headers.Type, out messageTypeString))
            {
                return null;
            }

            var type = Type.GetType(messageTypeString, false);

            if (type != null) return type;

            var message = $"Could not find .NET type matching '{messageTypeString}' - please be sure that the correct message assembly is available when handling messages";

            throw new SerializationException(message);
        }
    }
}
