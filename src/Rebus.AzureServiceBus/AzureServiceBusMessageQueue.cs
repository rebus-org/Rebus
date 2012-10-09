using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using Microsoft.WindowsAzure;
using Rebus.Logging;
using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Messaging;
using Rebus.Shared;

namespace Rebus.AzureServiceBus
{
    public class AzureServiceBusMessageQueue : ISendMessages, IReceiveMessages
    {

        static ILog log;
        NamespaceManager namespaceManager;
        string connectionString;


        static AzureServiceBusMessageQueue()
        {
            RebusLoggerFactory.Changed += f => log = f.GetCurrentClassLogger();
        }

        public AzureServiceBusMessageQueue(string connectionStringConfigurationName, string inputQueueName, string errorQueueName)
        {
            if (connectionStringConfigurationName == null)
                throw new ArgumentNullException("connectionStringConfigurationName");

            if (inputQueueName == null) throw new ArgumentNullException("inputQueueName");
            if (errorQueueName == null) throw new ArgumentNullException("errorQueueName");

            inputQueueName = inputQueueName.ToLowerInvariant();
            errorQueueName = errorQueueName.ToLowerInvariant();
            connectionString = CloudConfigurationManager.GetSetting(connectionStringConfigurationName);
            namespaceManager = NamespaceManager.CreateFromConnectionString(connectionString);

            if (!namespaceManager.QueueExists(inputQueueName))
            {
                namespaceManager.CreateQueue(inputQueueName);
            }
            var queueDescription = namespaceManager.GetQueue(inputQueueName);

            this.InputQueue = inputQueueName;
            this.ErrorQueue = errorQueueName;
            this.InputQueueAddress = queueDescription.Path;

        }
        public void Send(string destinationQueueName, TransportMessageToSend message, ITransactionContext context)
        {
            QueueClient client = QueueClient.CreateFromConnectionString(connectionString, destinationQueueName);

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

                var brokeredMessage = new BrokeredMessage(memoryStream.ToArray());
                brokeredMessage.Label = message.Label;
                brokeredMessage.CorrelationId = receivedTransportMessage.Id;

                var timeToLive = GetTimeToLive(message);
                if (timeToLive.HasValue)
                {
                    brokeredMessage.TimeToLive = timeToLive.Value;

                }

                client.Send(brokeredMessage);
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

            QueueClient client = QueueClient.CreateFromConnectionString(connectionString, InputQueue);

            BrokeredMessage message = client.Receive();

            if (message != null)
            {
                try
                {


                    var rawData = message.GetBody<byte[]>();

                    if (rawData == null)
                    {
                        log.Warn("Received message with NULL data - how weird is that?");
                        message.Complete();
                        return null;
                    }

                    using (var memoryStream = new MemoryStream(rawData))
                    {
                        var formatter = new BinaryFormatter();
                        var receivedTransportMessage = (ReceivedTransportMessage)formatter.Deserialize(memoryStream);
                        message.Complete();
                        return receivedTransportMessage;
                    }
                }
                catch (Exception e)
                {
                    log.Error(e, "An error occurred while receiving message from {0}", InputQueue);
                    message.Abandon();
                    return null;
                }
            }
            return null;
        }

        public string InputQueue { get; private set; }
        public string InputQueueAddress { get; private set; }
        public string ErrorQueue { get; private set; }
    }
}