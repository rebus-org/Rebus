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
            message.BodyStream = new MemoryStream(Encoding.UTF7.GetBytes(transportMessage.Data));
        }

        public object Read(Message message)
        {
            var stream = message.BodyStream;

            using (var reader = new StreamReader(stream, Encoding.UTF7))
            {
                return new ReceivedTransportMessage
                           {
                               Id = message.Id,
                               Data = reader.ReadToEnd()
                           };
            }
        }
    }
}