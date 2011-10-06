using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Messaging;
using System.Text;
using System.Transactions;
using Newtonsoft.Json;
using Rebus.Messages;

namespace Rebus.Msmq
{
    public class MsmqMessageQueue : ISendMessages, IReceiveMessages
    {
        readonly IProvideMessageTypes provideMessageTypes;
        readonly ConcurrentDictionary<string, MessageQueue> outputQueues = new ConcurrentDictionary<string, MessageQueue>();
        readonly MessageQueue inputQueue;
        readonly string inputQueuePath;

        [ThreadStatic]
        static MsmqTransactionWrapper currentTransaction;

        public MsmqMessageQueue(string inputQueuePath, IProvideMessageTypes provideMessageTypes)
        {
            this.inputQueuePath = inputQueuePath;
            this.provideMessageTypes = provideMessageTypes;
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
            messageQueue.Formatter = new JsonMessageFormatter();
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

    public class JsonMessageFormatter : IMessageFormatter
    {
        static readonly JsonSerializerSettings Settings = new JsonSerializerSettings{TypeNameHandling=TypeNameHandling.All};
        static readonly Encoding Encoding = Encoding.UTF8;

        public object Clone()
        {
            return this;
        }

        public bool CanRead(Message message)
        {
            return true;
        }

        public void Write(Message message, object obj)
        {
            var buffer = Encoding.GetBytes(JsonConvert.SerializeObject(obj, Formatting.Indented, Settings));

            // intentionally not disposing the stream - MSMQ does that
            var memoryStream = new MemoryStream(buffer);
            message.BodyStream = memoryStream;
            message.Label = ((TransportMessage) obj).GetLabel();
        }

        public object Read(Message message)
        {
            using(var reader = new StreamReader(message.BodyStream, Encoding))
            {
                return JsonConvert.DeserializeObject(reader.ReadToEnd(), Settings);
            }
        }
    }

    /// <summary>
    /// Wraps a <see cref="MessageQueueTransaction"/>, hooking it up to the ongoing
    /// ambient transaction if one is started, ignoring any calls to <see cref="Commit()"/>
    /// and <see cref="Abort"/>. If no ambient transaction was there, calls to
    /// <see cref="Commit()"/> and <see cref="Abort"/> will just be passed on to
    /// the wrapped transaction.
    /// </summary>
    public class MsmqTransactionWrapper : IEnlistmentNotification
    {
        readonly MessageQueueTransaction messageQueueTransaction;
        readonly bool enlistedInAmbientTx;

        public event Action Finished = delegate { };

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
            preparingEnlistment.Prepared();
        }

        public void Commit(Enlistment enlistment)
        {
            messageQueueTransaction.Commit();
            enlistment.Done();
            Finished();
        }

        public void Rollback(Enlistment enlistment)
        {
            messageQueueTransaction.Abort();
            enlistment.Done();
            Finished();
        }

        public void InDoubt(Enlistment enlistment)
        {
            messageQueueTransaction.Abort();
            enlistment.Done();
            Finished();
        }

        public void Begin()
        {
            // is begun already in the ctor
        }

        public void Commit()
        {
            if (enlistedInAmbientTx) return;
            messageQueueTransaction.Commit();
            Finished();
        }

        public void Abort()
        {
            if (enlistedInAmbientTx) return;
            messageQueueTransaction.Abort();
            Finished();
        }
    }
}