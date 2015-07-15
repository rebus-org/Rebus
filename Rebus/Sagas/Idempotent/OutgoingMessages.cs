using System.Collections.Generic;
using System.Linq;

namespace Rebus.Sagas.Idempotent
{
    public class OutgoingMessages
    {
        readonly List<OutgoingMessage> _messagesToSend;

        public OutgoingMessages(string messageId, IEnumerable<OutgoingMessage> messagesToSend)
        {
            MessageId = messageId;
            _messagesToSend = messagesToSend.ToList();
        }

        public string MessageId { get; private set; }

        public IEnumerable<OutgoingMessage> MessagesToSend
        {
            get { return _messagesToSend; }
        }

        public void Add(OutgoingMessage outgoingMessage)
        {
            _messagesToSend.Add(outgoingMessage);
        }
    }
}