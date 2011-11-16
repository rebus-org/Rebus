using System.Collections.Concurrent;
using System.Threading;
using Rebus.Messages;
using Rebus.Serialization.Json;

namespace Rebus.Tests
{
    public class MessageReceiverForTesting : IReceiveMessages
    {
        readonly JsonMessageSerializer serializer;
        readonly ConcurrentQueue<TransportMessageToSend> messageQueue = new ConcurrentQueue<TransportMessageToSend>();
        
        int idCounter;

        public MessageReceiverForTesting(JsonMessageSerializer serializer)
        {
            this.serializer = serializer;
        }

        public ReceivedTransportMessage ReceiveMessage()
        {
            TransportMessageToSend temp;
            if (messageQueue.TryDequeue(out temp))
            {
                return new ReceivedTransportMessage
                           {
                               Id = NewMessageId(),
                               Data = temp.Data
                           };
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