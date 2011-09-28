using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Messaging;

namespace Rebus
{
    public class MsmqMessageQueue : ISendMessages, IReceiveMessages
    {
        readonly IProvideMessageTypes provideMessageTypes;
        readonly ConcurrentDictionary<string, MessageQueue> outputQueues = new ConcurrentDictionary<string, MessageQueue>();
        readonly MessageQueue inputQueue;
        string inputQueuePath;

        public MsmqMessageQueue(string inputQueuePath, IProvideMessageTypes provideMessageTypes)
        {
            this.inputQueuePath = inputQueuePath;
            this.provideMessageTypes = provideMessageTypes;
            inputQueue = CreateMessageQueue(inputQueuePath, createIfNotExists: true);
        }

        public TransportMessage ReceiveMessage()
        {
            // TODO: enlist in ambient tx if one is present
            var messageQueueTransaction = new MessageQueueTransaction();
            var commit = true;
            try
            {
                messageQueueTransaction.Begin();
                var message = inputQueue.Receive(TimeSpan.FromSeconds(2), messageQueueTransaction);
                if (message == null)
                {
                    messageQueueTransaction.Commit();
                    return null;
                }
                var body = message.Body;
                if (body == null)
                {
                    messageQueueTransaction.Commit();
                    return null;
                }
                var transportMessage = (TransportMessage)body;
                messageQueueTransaction.Commit();
                return transportMessage;
            }
            catch(MessageQueueException)
            {
                messageQueueTransaction.Abort();
                return null;
            }
            catch (Exception)
            {
                messageQueueTransaction.Abort();
                return null;
            }
        }

        public string InputQueue
        {
            get { return inputQueuePath; }
        }

        public void Send(string recipient, TransportMessage message)
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
            var messageTypes = provideMessageTypes.GetMessageTypes().ToList();
            messageTypes.Add(typeof(TransportMessage));
            messageTypes.Add(typeof(SubscriptionMessage));
            messageQueue.Formatter = new XmlMessageFormatter(messageTypes.ToArray());
            return messageQueue;
        }

        MessageQueue GetMessageQueue(string path, bool createIfNotExists)
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