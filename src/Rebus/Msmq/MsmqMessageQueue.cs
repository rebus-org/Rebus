using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Messaging;
using System.Transactions;
using Rebus.Messages;

namespace Rebus.Msmq
{
    public class MsmqMessageQueue : ISendMessages, IReceiveMessages
    {
        readonly IProvideMessageTypes provideMessageTypes;
        readonly ConcurrentDictionary<string, MessageQueue> outputQueues = new ConcurrentDictionary<string, MessageQueue>();
        readonly MessageQueue inputQueue;
        readonly string inputQueuePath;

        public MsmqMessageQueue(string inputQueuePath, IProvideMessageTypes provideMessageTypes)
        {
            this.inputQueuePath = inputQueuePath;
            this.provideMessageTypes = provideMessageTypes;
            inputQueue = CreateMessageQueue(inputQueuePath, createIfNotExists: true);
        }

        public TransportMessage ReceiveMessage()
        {
            // TODO: enlist in ambient tx if one is present
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
            catch(MessageQueueException)
            {
                transactionWrapper.Abort();
                return null;
            }
            catch (Exception)
            {
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

    public class MsmqTransactionWrapper : ISinglePhaseNotification
    {
        readonly MessageQueueTransaction messageQueueTransaction;
        readonly bool enlistedInAmbientTx;

        public MsmqTransactionWrapper()
        {
            messageQueueTransaction = new MessageQueueTransaction();
            
            if (Transaction.Current != null)
            {
                enlistedInAmbientTx = true;
                Transaction.Current.EnlistVolatile(this, EnlistmentOptions.None);
            }

            messageQueueTransaction.Begin();
        }

        public MessageQueueTransaction MessageQueueTransaction
        {
            get { return messageQueueTransaction; }
        }

        public void Prepare(PreparingEnlistment preparingEnlistment)
        {
            Console.WriteLine("Prepare");
        }

        public void Commit(Enlistment enlistment)
        {
            Console.WriteLine("Commit");
            messageQueueTransaction.Commit();
        }

        public void Rollback(Enlistment enlistment)
        {
            Console.WriteLine("RollBack");
            messageQueueTransaction.Abort();
        }

        public void InDoubt(Enlistment enlistment)
        {
            Console.WriteLine("InDoubt");
        }

        public void SinglePhaseCommit(SinglePhaseEnlistment singlePhaseEnlistment)
        {
            Console.WriteLine("SinglePhaseCommit");
        }

        public void Begin()
        {
            if (enlistedInAmbientTx) return;
            messageQueueTransaction.Begin();
        }

        public void Commit()
        {
            if (enlistedInAmbientTx) return;
            messageQueueTransaction.Commit();
        }

        public void Abort()
        {
            if (enlistedInAmbientTx) return;
            messageQueueTransaction.Abort();
        }
    }
}