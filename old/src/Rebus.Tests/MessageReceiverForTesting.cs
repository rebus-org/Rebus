using System;
using System.Collections.Concurrent;
using System.Threading;
using Rebus.Messages;

namespace Rebus.Tests
{
    public class MessageReceiverForTesting : IReceiveMessages
    {
        readonly ISerializeMessages serializer;
        readonly ConcurrentQueue<ReceivedTransportMessage> messageQueue = new ConcurrentQueue<ReceivedTransportMessage>();
        
        int idCounter;
        string inputQueue;

        public MessageReceiverForTesting(ISerializeMessages serializer)
        {
            this.serializer = serializer;
        }

        public void Deliver(Message message)
        {
            var transportMessageToSend = serializer.Serialize(message);
            var receivedTransportMessage = transportMessageToSend.ToReceivedTransportMessage();

            messageQueue.Enqueue(receivedTransportMessage);
        }

        public ReceivedTransportMessage ReceiveMessage(ITransactionContext context)
        {
            ReceivedTransportMessage temp;
            if (messageQueue.TryDequeue(out temp))
            {
                context.DoRollback += () =>
                    {
                        Console.WriteLine("Returning {0} to the fake message queue", temp);
                        messageQueue.Enqueue(temp);
                    };

                return temp;
            }
            return null;
        }

        public string InputQueue
        {
            get { return inputQueue; }
        }

        public string InputQueueAddress
        {
            get { return InputQueue; }
        }

        public string ErrorQueue
        {
            get { return inputQueue + ".error"; }
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