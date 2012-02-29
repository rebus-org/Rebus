using System;
using System.IO;
using System.Messaging;
using System.Text;

namespace Rebus.Transports.Msmq
{
    /// <summary>
    /// MSMQ message formatter that should be capable of properly formatting MSMQ
    /// messages containins a raw byte stream.
    /// </summary>
    public class RebusTransportMessageFormatter : IMessageFormatter
    {
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
                throw new ArgumentException(string.Format("Object to serialize is not a TransportMessage - it's a {0}",
                                                          obj.GetType()));
            }
            message.BodyStream = new MemoryStream(transportMessage.Body);
        }

        public object Read(Message message)
        {
            var stream = message.BodyStream;

            using (var reader = new BinaryReader(stream))
            {
                return new ReceivedTransportMessage
                           {
                               Id = message.Id,
                               Body = reader.ReadBytes((int) stream.Length),
                           };
            }
        }
    }
}