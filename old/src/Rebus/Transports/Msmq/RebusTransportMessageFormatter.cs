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
        static readonly Encoding HeaderEcoding = Encoding.UTF7;

        internal static readonly MessagePropertyFilter PropertyFilter
            = new MessagePropertyFilter
                  {
                      Id = true,
                      Body = true,
                      Extension = true,
                      Label = true,
                  };

        static readonly DictionarySerializer DictionarySerializer = new DictionarySerializer();

        /// <summary>
        /// Returns this instance (it has no state)
        /// </summary>
        public object Clone()
        {
            return this;
        }

        /// <summary>
        /// Always returns true - we always want to attempt to read the message
        /// </summary>
        public bool CanRead(Message message)
        {
            return true;
        }

        /// <summary>
        /// Writes to the MSMQ message, assuming that the given object is a <see cref="TransportMessageToSend"/> -
        /// otherwise, an <see cref="ArgumentException"/> will be thrown
        /// </summary>
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

            var hasTimeout = transportMessage.Headers.ContainsKey(Headers.TimeToBeReceived);

            // make undelivered messages go to the dead letter queue if they could disappear from the queue anyway
            message.UseDeadLetterQueue = !(expressDelivery || hasTimeout);
            message.Recoverable = !expressDelivery;

            if (hasTimeout)
            {
                var timeToBeReceivedStr = (string)transportMessage.Headers[Headers.TimeToBeReceived];
                message.TimeToBeReceived = TimeSpan.Parse(timeToBeReceivedStr);
            }
        }

        /// <summary>
        /// Reads the given MSMQ message, wrapping the message in a <see cref="ReceivedTransportMessage"/>
        /// </summary>
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