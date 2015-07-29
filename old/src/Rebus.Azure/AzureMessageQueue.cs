using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.StorageClient;
using Rebus.Logging;
using Rebus.Shared;

namespace Rebus.Azure
{
    public class AzureMessageQueue : ISendMessages, IReceiveMessages
    {
        static ILog log;

        static AzureMessageQueue()
        {
            RebusLoggerFactory.Changed += f => log = f.GetCurrentClassLogger();
        }

        readonly string inputQueueName;
        readonly string errorQueueName;
        readonly CloudQueueClient cloudQueueClient;
        readonly CloudQueue inputQueue;

        public AzureMessageQueue(CloudStorageAccount cloudStorageAccount, string inputQueueName, string errorQueueName)
        {
            if (inputQueueName == null) throw new ArgumentNullException("inputQueueName");
            if (errorQueueName == null) throw new ArgumentNullException("errorQueueName");

            inputQueueName = inputQueueName.ToLowerInvariant();
            errorQueueName = errorQueueName.ToLowerInvariant();

            cloudQueueClient = cloudStorageAccount.CreateCloudQueueClient();

            inputQueue = cloudQueueClient.GetQueueReference(inputQueueName);
            inputQueue.CreateIfNotExist();

            cloudQueueClient.GetQueueReference(errorQueueName).CreateIfNotExist();

            this.inputQueueName = inputQueueName;
            this.errorQueueName = errorQueueName;
        }

        public void Send(string destinationQueueName, TransportMessageToSend message, ITransactionContext context)
        {
            var outputQueue = cloudQueueClient.GetQueueReference(destinationQueueName);

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
                return TimeSpan.Parse((string)message.Headers[Headers.TimeToBeReceived]);
            }

            return null;
        }

        public ReceivedTransportMessage ReceiveMessage(ITransactionContext context)
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
