using System;
using System.IO;
using System.Messaging;

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

        public object Read(Message message)
        {
            var stream = message.BodyStream;

            using (var reader = new BinaryReader(stream))
            {
                return new TransportMessage
                           {
                               Id = message.Id,
                               Data = reader.ReadBytes((int) stream.Length)
                           };
            }
        }

        public void Write(Message message, object obj)
        {
            var transportMessage = obj as TransportMessage;
            if (transportMessage == null)
            {
                throw new ArgumentException(string.Format("Object to serialize is not a TransportMessage - it's a {0}",
                                                          obj.GetType()));
            }
            message.BodyStream = new MemoryStream(transportMessage.Data);
        }
    }
}