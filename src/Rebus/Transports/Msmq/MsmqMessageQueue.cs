using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Messaging;
using System.Reflection;
using System.Text;
using Rebus.Logging;

namespace Rebus.Transports.Msmq
{
    /// <summary>
    /// MSMQ implementation of <see cref="ISendMessages"/> and <see cref="IReceiveMessages"/>. Will
    /// enlist in ambient transaction during send and receive if one is present. Uses JSON serialization
    /// of objects in messages as default.
    /// </summary>
    public class MsmqMessageQueue : ISendMessages, IReceiveMessages, IDisposable
    {
        static readonly ILog Log = RebusLoggerFactory.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        static readonly Encoding HeaderEcoding = Encoding.UTF7;

        readonly ConcurrentDictionary<string, MessageQueue> outputQueues = new ConcurrentDictionary<string, MessageQueue>();
        readonly DictionarySerializer dictionarySerializer = new DictionarySerializer();
        readonly MessageQueue inputQueue;
        readonly string inputQueuePath;

        [ThreadStatic]
        static MsmqTransactionWrapper currentTransaction;

        public static string PrivateQueue(string queueName)
        {
            return string.Format(@".\private$\{0}", queueName);
        }

        public MsmqMessageQueue(string inputQueuePath)
        {
            this.inputQueuePath = inputQueuePath;
            inputQueue = CreateMessageQueue(inputQueuePath, createIfNotExists: true);
        }

        public ReceivedTransportMessage ReceiveMessage()
        {
            var transactionWrapper = new MsmqTransactionWrapper();

            try
            {
                transactionWrapper.Begin();
                var message = inputQueue.Receive(TimeSpan.FromSeconds(2), transactionWrapper.MessageQueueTransaction);
                if (message == null)
                {
                    Log.Warn("Received NULL message - how weird is that?");
                    transactionWrapper.Commit();
                    return null;
                }
                var body = message.Body;
                if (body == null)
                {
                    Log.Warn("Received message with NULL body - how weird is that?");
                    transactionWrapper.Commit();
                    return null;
                }
                var transportMessage = (ReceivedTransportMessage) body;
                transportMessage.Headers = dictionarySerializer.Deserialize(HeaderEcoding.GetString(message.Extension));
                transactionWrapper.Commit();
                return transportMessage;
            }
            catch (MessageQueueException)
            {
                transactionWrapper.Abort();
                return null;
            }
            catch (Exception e)
            {
                Log.Error(e, "An error occurred while receiving message from {0}", inputQueuePath);
                transactionWrapper.Abort();
                return null;
            }
        }

        public string InputQueue
        {
            get { return inputQueuePath; }
        }

        public void Send(string recipient, TransportMessageToSend message)
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
            var msmqMessage = CreateMessage(message, outputQueue);
            
            outputQueue.Send(msmqMessage, transactionWrapper.MessageQueueTransaction);
            
            transactionWrapper.Commit();
        }

        public MsmqMessageQueue PurgeInputQueue()
        {
            Log.Warn("Purging {0}", inputQueuePath);
            inputQueue.Purge();
            return this;
        }

        public void Dispose()
        {
            Log.Info("Disposing message queues");
            inputQueue.Dispose();
            outputQueues.Values.ToList().ForEach(q => q.Dispose());
        }

        public override string ToString()
        {
            return string.Format("MsmqMessageQueue: {0}", inputQueuePath);
        }

        Message CreateMessage(TransportMessageToSend message, MessageQueue outputQueue)
        {
            var msmqMessage = new Message();
            outputQueue.Formatter.Write(msmqMessage, message);

            SetLabel(message, msmqMessage);

            if (message.Headers == null) return msmqMessage;

            SetHeaders(message, msmqMessage);

            return msmqMessage;
        }

        void SetHeaders(TransportMessageToSend message, Message msmqMessage)
        {
            msmqMessage.Extension = HeaderEcoding.GetBytes(dictionarySerializer.Serialize(message.Headers));

            if (message.Headers.ContainsKey("TimeToBeReceived"))
            {
                msmqMessage.TimeToBeReceived = TimeSpan.Parse(message.Headers["TimeToBeReceived"]);
            }
        }

        void SetLabel(TransportMessageToSend message, Message msmqMessage)
        {
            var label = message.Label;
            if (!string.IsNullOrWhiteSpace(label))
            {
                msmqMessage.Label = label;
            }
        }

        MsmqTransactionWrapper GetOrCreateTransactionWrapper()
        {
            if (currentTransaction != null)
            {
                return currentTransaction;
            }

            currentTransaction = new MsmqTransactionWrapper();
            currentTransaction.Finished += () => currentTransaction = null;

            return currentTransaction;
        }

        MessageQueue CreateMessageQueue(string path, bool createIfNotExists)
        {
            var messageQueue = GetMessageQueue(path, createIfNotExists);
            messageQueue.Formatter = new RebusTransportMessageFormatter();
            var messageReadPropertyFilter = new MessagePropertyFilter();
            messageReadPropertyFilter.Id = true;
            messageReadPropertyFilter.Body = true;
            messageReadPropertyFilter.Extension = true;
            messageQueue.MessageReadPropertyFilter = messageReadPropertyFilter;
            return messageQueue;
        }

        MessageQueue GetMessageQueue(string path, bool createIfNotExists)
        {
            var queueExists = MessageQueue.Exists(path);

            if (!queueExists && createIfNotExists)
            {
                Log.Info("MSMQ queue {0} does not exist - it will be created now...", path);
                return MessageQueue.Create(path, true);
            }

            return new MessageQueue(path);
        }
    }
}