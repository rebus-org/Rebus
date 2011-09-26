using System;
using System.Collections.Concurrent;
using System.Messaging;

namespace Rebus
{
    public class MsmqMessageQueue : ISendMessages, IReceiveMessages
    {
        readonly IProvideMessageTypes provideMessageTypes;
        readonly ConcurrentDictionary<string, MessageQueue> outputQueues = new ConcurrentDictionary<string, MessageQueue>();
        readonly MessageQueue inputQueue;

        public MsmqMessageQueue(string inputQueuePath, IProvideMessageTypes provideMessageTypes)
        {
            this.provideMessageTypes = provideMessageTypes;
            inputQueue = CreateMessageQueue(inputQueuePath, createIfNotExists: true);
        }

        public object ReceiveMessage()
        {
            try
            {
                var message = inputQueue.Receive(TimeSpan.FromSeconds(2));
                if (message == null) return null;
                return message.Body;
            }
            catch(MessageQueueException e)
            {
                return null;
            }
        }

        public void Send(string recipient, object message)
        {
            MessageQueue outputQueue;
            if (!outputQueues.TryGetValue(recipient, out outputQueue))
            {
                lock (outputQueues)
                {
                    if (!outputQueues.TryGetValue(recipient, out outputQueue))
                    {
                        outputQueue = CreateMessageQueue(recipient, createIfNotExists: false);
                        outputQueues[recipient] = outputQueue;
                    }
                }
            }

            // TODO: enlist in ambient tx if one is present
            var messageQueueTransaction = new MessageQueueTransaction();
            messageQueueTransaction.Begin();
            outputQueue.Send(message, messageQueueTransaction);
            messageQueueTransaction.Commit();
        }

        MessageQueue CreateMessageQueue(string path, bool createIfNotExists)
        {
            var messageQueue = GetMessageQueue(path, createIfNotExists);
            messageQueue.Formatter = new XmlMessageFormatter(provideMessageTypes.GetMessageTypes());
            return messageQueue;
        }

        static MessageQueue GetMessageQueue(string path, bool createIfNotExists)
        {
            var queueExists = MessageQueue.Exists(path);

            if (!queueExists && createIfNotExists)
            {
                return MessageQueue.Create(path, true);
            }

            return new MessageQueue(path);
        }
    }
}