using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.StorageClient;
using Rebus.Extensions;
using Rebus.Logging;
using Rebus.Messages;
using Rebus.Serialization;
using Rebus.Shared;
using System.Linq;

namespace Rebus.Azure
{
    public class AzureMessageQueue : ISendMessages, IReceiveMessages
    {
        static ILog log;

        static AzureMessageQueue()
        {
            RebusLoggerFactory.Changed += f => log = f.GetCurrentClassLogger();
        }

        static readonly Encoding Encoding = Encoding.UTF7;

        readonly string inputQueueName;
        readonly string errorQueueName;
        readonly CloudQueueClient cloudQueueClient;
        readonly ConcurrentDictionary<string, CloudQueue> outputQueues = new ConcurrentDictionary<string, CloudQueue>();
        readonly CloudQueue inputQueue;
        readonly DictionarySerializer dictionarySerializer;

        public AzureMessageQueue(CloudStorageAccount cloudStorageAccount, string inputQueueName, string errorQueueName)
        {
            this.inputQueueName = inputQueueName.ToLowerInvariant();
            this.errorQueueName = errorQueueName.ToLowerInvariant();

            cloudQueueClient = cloudStorageAccount.CreateCloudQueueClient();
            dictionarySerializer = new DictionarySerializer();

            inputQueue = cloudQueueClient.GetQueueReference(this.inputQueueName);
            inputQueue.CreateIfNotExist();
            cloudQueueClient.GetQueueReference(this.errorQueueName).CreateIfNotExist();
        }

        public void Send(string destinationQueueName, TransportMessageToSend message)
        {
            CloudQueue outputQueue;

            if (!outputQueues.TryGetValue(destinationQueueName, out outputQueue))
            {
                lock (outputQueues)
                {
                    if (!outputQueues.TryGetValue(destinationQueueName, out outputQueue))
                    {
                        outputQueue = cloudQueueClient.GetQueueReference(destinationQueueName);
                        outputQueue.CreateIfNotExist();
                        outputQueues[destinationQueueName] = outputQueue;
                    }
                }
            }

            using (var memoryStream = new MemoryStream())
            {
                var formatter = new BinaryFormatter();
                var receivedTransportMessage = new ReceivedTransportMessage
                    {
                        Id = Guid.NewGuid().ToString(),
                        Headers = message.Headers,
                        Body = message.Body,
                        Label = message.Label,
                    };

                formatter.Serialize(memoryStream, receivedTransportMessage);
                memoryStream.Position = 0;

                var cloudQueueMessage = new CloudQueueMessage(memoryStream.ToArray());

                var timeToLive = GetTimeToLive(message);
                if (timeToLive.HasValue)
                {
                    outputQueue.AddMessage(cloudQueueMessage, timeToLive.Value);
                }
                else
                {
                    outputQueue.AddMessage(cloudQueueMessage);
                }
            }
        }

        private TimeSpan? GetTimeToLive(TransportMessageToSend message)
        {
            if (message.Headers != null && message.Headers.ContainsKey(Headers.TimeToBeReceived))
            {
                return TimeSpan.Parse(message.Headers[Headers.TimeToBeReceived]);
            }

            return null;
        }

        public ReceivedTransportMessage ReceiveMessage()
        {
            var azureMessageQueueTransactionSimulator = new AzureMessageQueueTransactionSimulator(inputQueue);
            try
            {
                var message = azureMessageQueueTransactionSimulator.RetrieveCloudQueueMessage = inputQueue.GetMessage();

                if (message == null)
                {
                    //No message receieved
                    azureMessageQueueTransactionSimulator.Commit();
                    return null;
                }

                var rawData = message.AsBytes;

                if (rawData == null)
                {
                    log.Warn("Received message with NULL data - how weird is that?");
                    azureMessageQueueTransactionSimulator.Commit();
                    return null;
                }

                using (var memoryStream = new MemoryStream(rawData))
                {
                    var formatter = new BinaryFormatter();
                    var receivedTransportMessage = (ReceivedTransportMessage)formatter.Deserialize(memoryStream);
                    azureMessageQueueTransactionSimulator.Commit();
                    return receivedTransportMessage;
                }
            }
            catch (Exception e)
            {
                log.Error(e, "An error occurred while receiving message from {0}", inputQueueName);
                azureMessageQueueTransactionSimulator.Abort();
                return null;
            }
        }

        public string InputQueue
        {
            get { return inputQueueName; }
        }

        public string InputQueueAddress
        {
            get { return InputQueue; }
        }

        public string ErrorQueue
        {
            get { return errorQueueName; }
        }

        public AzureMessageQueue PurgeInputQueue()
        {
            log.Warn("Purging {0}", inputQueueName);

            if (inputQueue.Exists())
                inputQueue.Clear();

            return this;
        }
    }
}
