// Copyright 2011 Mogens Heller Grabe
// 
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in
// compliance with the License. You may obtain a copy of the License at
// 
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software distributed under the License is
// distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and limitations under the License.
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Messaging;

namespace Rebus.Transports.Msmq
{
    /// <summary>
    /// MSMQ implementation of <see cref="ISendMessages"/> and <see cref="IReceiveMessages"/>. Will
    /// enlist in ambient transaction during send and receive if one is present. Uses JSON serialization
    /// of objects in messages as default.
    /// </summary>
    public class MsmqMessageQueue : ISendMessages, IReceiveMessages, IDisposable
    {
        readonly ConcurrentDictionary<string, MessageQueue> outputQueues = new ConcurrentDictionary<string, MessageQueue>();
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
                    transactionWrapper.Commit();
                    return null;
                }
                var body = message.Body;
                if (body == null)
                {
                    transactionWrapper.Commit();
                    return null;
                }
                var transportMessage = (ReceivedTransportMessage) body;
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
                Console.WriteLine(e);
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
            outputQueue.Send(CreateMessage(message, outputQueue), transactionWrapper.MessageQueueTransaction);
            transactionWrapper.Commit();
        }

        public MsmqMessageQueue PurgeInputQueue()
        {
            inputQueue.Purge();
            return this;
        }

        public void Dispose()
        {
            inputQueue.Dispose();
            outputQueues.Values.ToList().ForEach(q => q.Dispose());
        }

        Message CreateMessage(TransportMessageToSend message, MessageQueue outputQueue)
        {
            var msmqMessage = new Message();
            outputQueue.Formatter.Write(msmqMessage, message);

            if (message.Headers == null) return msmqMessage;

            if (message.Headers.ContainsKey("TimeToBeReceived"))
            {
                msmqMessage.TimeToBeReceived = TimeSpan.Parse(message.Headers["TimeToBeReceived"]);
            }
            
            return msmqMessage;
        }

        MsmqTransactionWrapper GetOrCreateTransactionWrapper()
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
            messageQueue.Formatter = new RebusTransportMessageFormatter();
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