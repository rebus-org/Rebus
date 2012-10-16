using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading;
using Rebus.Logging;
using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Messaging;
using Rebus.Shared;

namespace Rebus.AzureServiceBus
{
    public class AzureServiceBusMessageQueue : ISendMessages, IReceiveMessages
    {
        const string AzureServiceBusMessageQueueContextKey = "AzureServiceBusMessageQueueContextKey";

        static ILog log;
        static AzureServiceBusMessageQueue()
        {
            RebusLoggerFactory.Changed += f => log = f.GetCurrentClassLogger();
        }

        readonly ThreadLocal<Queue<BrokeredMessage>> messagesReceived = new ThreadLocal<Queue<BrokeredMessage>>(() => new Queue<BrokeredMessage>());
        readonly ThreadLocal<Queue<Tuple<string, BrokeredMessage>>> messagesToSend = new ThreadLocal<Queue<Tuple<string, BrokeredMessage>>>(() => new Queue<Tuple<string, BrokeredMessage>>());
        readonly MessagingFactory messagingFactory;
        readonly NamespaceManager namespaceManager;
        readonly QueueClient receiverQueueClient;

        public AzureServiceBusMessageQueue(string connectionString, string inputQueueName, string errorQueueName)
        {
            if (connectionString == null) throw new ArgumentNullException("connectionString");

            if (inputQueueName == null) throw new ArgumentNullException("inputQueueName");
            if (errorQueueName == null) throw new ArgumentNullException("errorQueueName");
            inputQueueName = inputQueueName.ToLowerInvariant();
            errorQueueName = errorQueueName.ToLowerInvariant();
            
            // connectionString = CloudConfigurationManager.GetSetting(connectionStringConfigurationName);
            namespaceManager = NamespaceManager.CreateFromConnectionString(connectionString);

            if (!namespaceManager.QueueExists(inputQueueName))
            {
                namespaceManager.CreateQueue(inputQueueName);
            }
            if (!namespaceManager.QueueExists(errorQueueName))
            {
                namespaceManager.CreateQueue(errorQueueName);
            }

            messagingFactory = MessagingFactory.CreateFromConnectionString(connectionString);
            receiverQueueClient = QueueClient.CreateFromConnectionString(connectionString, inputQueueName);
            
            InputQueue = inputQueueName;
            ErrorQueue = errorQueueName;
            InputQueueAddress = inputQueueName;
        }

        private void EnsureTransactionEvents(ITransactionContext context, string queueSuffix)
        {
            if (context.IsTransactional)
            {
                if (context[AzureServiceBusMessageQueueContextKey + queueSuffix] == null)
                {
                    context[AzureServiceBusMessageQueueContextKey + queueSuffix] = true;
                    context.DoCommit += () =>
                    {
                        if (messagesToSend.Value.Count > 0)
                        {
                            while (messagesToSend.Value.Count > 0)
                            {
                                var destinationAndMessage = messagesToSend.Value.Dequeue();
                                var client = messagingFactory.CreateQueueClient(destinationAndMessage.Item1);
                                client.Send(destinationAndMessage.Item2);
                            }

                        }

                        if (messagesReceived.Value.Count > 0)
                        {
                            while (messagesReceived.Value.Count > 0)
                            {
                                var message = messagesReceived.Value.Dequeue();
                                message.Complete();

                            }
                        }


                    };
                    context.DoRollback += () =>
                    {
                        if (messagesReceived.Value.Count > 0)
                        {
                            while (messagesReceived.Value.Count > 0)
                            {
                                var message = messagesReceived.Value.Dequeue();
                                message.Abandon();

                            }
                        }
                        messagesToSend.Value.Clear();

                    };
                    context.Cleanup += () =>
                        {
                            messagingFactory.Close();
                            
                        };

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
        public void Send(string destinationQueueName, TransportMessageToSend message, ITransactionContext context)
        {

            var client = messagingFactory.CreateQueueClient(destinationQueueName);

            EnsureTransactionEvents(context, "sender");


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

                if (!context.IsTransactional)
                {
                    client.Send(brokeredMessage);
                }
                else
                {
                    messagesToSend.Value.Enqueue(new Tuple<string, BrokeredMessage>(destinationQueueName, brokeredMessage));
                }
            }
        }

        public ReceivedTransportMessage ReceiveMessage(ITransactionContext context)
        {



            EnsureTransactionEvents(context, "receiver");
            BrokeredMessage message = null;
            if (context.IsTransactional)
            {
                var queueClientTransaction = messagingFactory.CreateQueueClient(InputQueue);
                message = queueClientTransaction.Receive(TimeSpan.FromSeconds(2));
            }
            else
            {
                message = receiverQueueClient.Receive(TimeSpan.FromSeconds(2));
            }


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

                        if (!context.IsTransactional)
                        {
                            message.Complete();
                        }
                        else
                        {
                            messagesReceived.Value.Enqueue(message);
                        }
                        return receivedTransportMessage;
                    }
                }
                catch (Exception e)
                {
                    log.Error(e, "An error occurred while receiving message from {0}", InputQueue);
                    if (!context.IsTransactional)
                    {
                        message.Abandon();
                    }

                    return null;
                }
            }
            return null;
        }



        public string InputQueue { get; private set; }
        public string InputQueueAddress { get; private set; }
        public string ErrorQueue { get; private set; }

        public AzureServiceBusMessageQueue ResetQueue()
        {
            namespaceManager.DeleteQueue(InputQueue);
            namespaceManager.CreateQueue(InputQueue);

            return this;
        }
    }
}