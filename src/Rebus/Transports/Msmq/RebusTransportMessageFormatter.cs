using System;
using System.IO;
using System.Messaging;
using System.Text;
using Rebus.Serialization;
using Rebus.Shared;
using Message = System.Messaging.Message;

namespace Rebus.Transports.Msmq
{
    /// <summary>
    /// MSMQ message formatter that should be capable of properly formatting MSMQ
    /// messages containins a raw byte stream.
    /// </summary>
    public class RebusTransportMessageFormatter : IMessageFormatter
    {
        public static readonly Encoding HeaderEcoding = Encoding.UTF7;

        public static MessagePropertyFilter PropertyFilter
            = new MessagePropertyFilter
                {
                    Id = true,
                    Body = true,
                    Extension = true,
                    Label = true,
                };

        static readonly DictionarySerializer DictionarySerializer = new DictionarySerializer();

        public object Clone()
        {
            return this;
        }

        public bool CanRead(Message message)
        {
            return true;
        }

        public void Write(Message message, object obj)
        {
            var transportMessage = obj as TransportMessageToSend;
            if (transportMessage == null)
            {
                throw new ArgumentException(
                    string.Format("Object to serialize is not a TransportMessageToSend - it's a {0}",
                                  obj.GetType()));
            }
            message.BodyStream = new MemoryStream(transportMessage.Body);
            message.Extension = HeaderEcoding.GetBytes(DictionarySerializer.Serialize(transportMessage.Headers));
            message.Label = transportMessage.Label ?? "???";

            var expressDelivery = transportMessage.Headers.ContainsKey(Headers.Express);

            message.UseDeadLetterQueue = !expressDelivery;
            message.Recoverable = !expressDelivery;

            if (transportMessage.Headers.ContainsKey(Headers.TimeToBeReceived))
            {
                var timeToBeReceivedStr = transportMessage.Headers[Headers.TimeToBeReceived];
                message.TimeToBeReceived = TimeSpan.Parse(timeToBeReceivedStr);
            }
        }

        public object Read(Message message)
        {
            var stream = message.BodyStream;

            using (var reader = new BinaryReader(stream))
            {
                return new ReceivedTransportMessage
                           {
                               Id = message.Id,
                               Body = reader.ReadBytes((int)stream.Length),
                               Label = message.Label,
                               Headers = DictionarySerializer.Deserialize(HeaderEcoding.GetString(message.Extension))
                           };
            }
        }
    }
}