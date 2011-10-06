using System.IO;
using System.Messaging;
using System.Text;
using Rebus.Messages;

namespace Rebus.Transports.Msmq
{
    /// <summary>
    /// MSMQ message formatter that should be capable of properly formatting MSMQ
    /// messages containing <see cref="TransportMessage"/>s, using the specified
    /// serializer.
    /// </summary>
    public class RebusTransportMessageFormatter : IMessageFormatter
    {
        readonly IMessageSerializer messageSerializer;
        static readonly Encoding Encoding = Encoding.UTF8;

        public RebusTransportMessageFormatter(IMessageSerializer messageSerializer)
        {
            this.messageSerializer = messageSerializer;
        }

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
            using (var reader = new StreamReader(message.BodyStream, Encoding))
                return messageSerializer.Deserialize(reader.ReadToEnd());
        }

        public void Write(Message message, object obj)
        {
            message.BodyStream = new MemoryStream(Encoding.GetBytes(messageSerializer.Serialize(obj)));
            message.Label = ((TransportMessage) obj).GetLabel();
        }
    }
}