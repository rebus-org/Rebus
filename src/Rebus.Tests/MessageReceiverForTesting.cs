using System.Collections.Concurrent;
using System.Threading;
using Rebus.Messages;

namespace Rebus.Tests
{
    public class MessageReceiverForTesting : IReceiveMessages
    {
        readonly ISerializeMessages serializer;
        readonly ConcurrentQueue<TransportMessageToSend> messageQueue = new ConcurrentQueue<TransportMessageToSend>();
        
        int idCounter;
        string inputQueue;

        public MessageReceiverForTesting(ISerializeMessages serializer)
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
            get { return inputQueue; }
        }

        public void Deliver(Message message)
        {
            messageQueue.Enqueue(serializer.Serialize(message));
        }

        string NewMessageId()
        {
            return string.Format("Message#{0000}", Interlocked.Increment(ref idCounter));
        }

        public void SetInputQueue(string myInputQueue)
        {
            inputQueue = myInputQueue;
        }
    }
}