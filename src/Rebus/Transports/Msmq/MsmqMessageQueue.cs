using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Messaging;
using Rebus.Messages;

namespace Rebus.Transports.Msmq
{
    /// <summary>
    /// MSMQ implementation of <see cref="ISendMessages"/> and <see cref="IReceiveMessages"/>. Will
    /// enlist in ambient transaction during send and receive if one is present. Uses JSON serialization
    /// of objects in messages as default.
    /// </summary>
    public class MsmqMessageQueue : ISendMessages, IReceiveMessages
    {
        readonly IProvideMessageTypes provideMessageTypes;
        readonly IMessageSerializer messageSerializer;
        readonly ConcurrentDictionary<string, MessageQueue> outputQueues = new ConcurrentDictionary<string, MessageQueue>();
        readonly MessageQueue inputQueue;
        readonly string inputQueuePath;

        [ThreadStatic]
        static MsmqTransactionWrapper currentTransaction;

        public MsmqMessageQueue(string inputQueuePath, IProvideMessageTypes provideMessageTypes, IMessageSerializer messageSerializer)
        {
            this.inputQueuePath = inputQueuePath;
            this.provideMessageTypes = provideMessageTypes;
            this.messageSerializer = messageSerializer;
            inputQueue = CreateMessageQueue(inputQueuePath, createIfNotExists: true);
        }

        public TransportMessage ReceiveMessage()
        {
            var transactionWrapper = new MsmqTransactionWrapper();

            try
            {
                transactionWrapper.Begin();
                var message = inputQueue.Receive(TimeSpan.FromSeconds(2), transactionWrapper.MessageQueueTransaction);
                if (message == null)
                {
                    transactionWrapper.Commit();
                    return null;
                }
                var body = message.Body;
                if (body == null)
                {
                    transactionWrapper.Commit();
                    return null;
                }
                var transportMessage = (TransportMessage)body;
                transactionWrapper.Commit();
                return transportMessage;
            }
            catch (MessageQueueException e)
            {
                transactionWrapper.Abort();
                return null;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                transactionWrapper.Abort();
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

            var transactionWrapper = GetOrCreateTransactionWrapper();
            outputQueue.Send(message, transactionWrapper.MessageQueueTransaction);
            transactionWrapper.Commit();
        }

        static MsmqTransactionWrapper GetOrCreateTransactionWrapper()
        {
            if (currentTransaction != null)
                return currentTransaction;

            currentTransaction = new MsmqTransactionWrapper();
            currentTransaction.Finished += () => currentTransaction = null;

            return currentTransaction;
        }

        MessageQueue CreateMessageQueue(string path, bool createIfNotExists)
        {
            var messageQueue = GetMessageQueue(path, createIfNotExists);
            var messageTypes = provideMessageTypes.GetMessageTypes().ToList();
            messageTypes.Add(typeof(TransportMessage));
            messageTypes.Add(typeof(SubscriptionMessage));
            messageQueue.Formatter = new BinaryMessageFormatter();
            messageQueue.Formatter = new RebusTransportMessageFormatter(messageSerializer);
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

        public static string PrivateQueue(string queueName)
        {
            return string.Format(@".\private$\{0}", queueName);
        }

        public MsmqMessageQueue PurgeInputQueue()
        {
            inputQueue.Purge();
            return this;
        }
    }
}