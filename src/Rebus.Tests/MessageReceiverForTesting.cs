using System.Collections.Concurrent;
using System.Threading;
using Rebus.Messages;
using Rebus.Serialization.Json;

namespace Rebus.Tests
{
    public class MessageReceiverForTesting : IReceiveMessages
    {
        readonly JsonMessageSerializer serializer;
        readonly ConcurrentQueue<TransportMessage> messageQueue = new ConcurrentQueue<TransportMessage>();
        
        int idCounter;

        public MessageReceiverForTesting(JsonMessageSerializer serializer)
        {
            this.serializer = serializer;
        }

        public TransportMessage ReceiveMessage()
        {
            TransportMessage temp;
            if (messageQueue.TryDequeue(out temp))
            {
                temp.Id = NewMessageId();
                return temp;
            }
            return null;
        }

        public string InputQueue
        {
            get { return "message_receiver_for_testing"; }
        }

        public void Deliver(Message message)
        {
            messageQueue.Enqueue(serializer.Serialize(message));
        }

        string NewMessageId()
        {
            return string.Format("Message#{0000}", Interlocked.Increment(ref idCounter));
        }
    }
}