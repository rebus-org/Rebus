using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Transactions;
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
            var receivedTransportMessage = new ReceivedTransportMessage
                                               {
                                                   Id = NewMessageId(),
                                                   Body = transportMessageToSend.Body,
                                                   Label = transportMessageToSend.Label,
                                               };

            messageQueue.Enqueue(receivedTransportMessage);
        }

        public ReceivedTransportMessage ReceiveMessage()
        {
            ReceivedTransportMessage temp;
            if (messageQueue.TryDequeue(out temp))
            {
                if (Transaction.Current != null)
                {
                    // simulate that delivery was rolled back by putting the message back
                    var txHook = new TxHook()
                        .OnRollback(() =>
                                        {
                                            Console.WriteLine("Returning {0} to the fake message queue", temp);
                                            messageQueue.Enqueue(temp);
                                        });

                    Transaction.Current.EnlistVolatile(txHook, EnlistmentOptions.None);
                }

                return temp;
            }
            return null;
        }

        class TxHook : IEnlistmentNotification
        {
            readonly List<Action> rollbackActions = new List<Action>();

            public TxHook OnRollback(Action action)
            {
                rollbackActions.Add(action);
                return this;
            }

            public void Prepare(PreparingEnlistment preparingEnlistment)
            {
                preparingEnlistment.Prepared();
            }

            public void Commit(Enlistment enlistment)
            {
                enlistment.Done();
            }

            public void Rollback(Enlistment enlistment)
            {
                rollbackActions.ForEach(a => a());
                enlistment.Done();
            }

            public void InDoubt(Enlistment enlistment)
            {
                enlistment.Done();
            }
        }

        public string InputQueue
        {
            get { return inputQueue; }
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