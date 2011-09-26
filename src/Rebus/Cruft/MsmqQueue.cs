using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Messaging;

namespace Rebus.Cruft
{
    public class MsmqQueue : IQueue
    {
        readonly IProvideMessageTypes provideMessageTypes;
        readonly string inputQueue;

        readonly ConcurrentDictionary<string, Msmq.Sender> senders = new ConcurrentDictionary<string, Msmq.Sender>();
        readonly List<Worker> workers = new List<Worker>();
        MessageQueueTransaction tx;
        MessageQueue messageQueue;

        public MsmqQueue(IProvideMessageTypes provideMessageTypes, string inputQueue)
        {
            this.provideMessageTypes = provideMessageTypes;
            this.inputQueue = inputQueue;

            messageQueue = MessageQueue.Exists(inputQueue)
                       ? new MessageQueue(inputQueue)
                       : MessageQueue.Create(inputQueue, transactional: true);

            messageQueue.Formatter = new XmlMessageFormatter(provideMessageTypes.GetMessageTypes());
        }

        public ISendMessages GetSender(string endpoint)
        {
            Msmq.Sender sender;

            if (!senders.TryGetValue(endpoint, out sender))
            {
                lock (senders)
                {
                    if (!senders.TryGetValue(endpoint, out sender))
                        sender = new Msmq.Sender(messageQueue);

                    senders[endpoint] = sender;
                }
            }

            if (tx == null)
            {
                tx = new MessageQueueTransaction();
                tx.Begin();
            }

            return new InternalSender(message => sender.Send(message, tx));
        }

        class InternalSender : ISendMessages
        {
            readonly Action<object> sendAction;

            public InternalSender(Action<object> sendAction)
            {
                this.sendAction = sendAction;
            }

            public void Send(object message)
            {
                sendAction(message);
            }
        }

        public IReceiveMessages GetReceiver()
        {
            return new MsmqReceiveMessages(inputQueue, provideMessageTypes);
        }
    }
}