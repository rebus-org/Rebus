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
    public class AzureServiceBusMessageQueue : IDuplexTransport, IDisposable
    {
        const string AzureServiceBusMessageQueueContextKey = "AzureServiceBusMessageQueueContextKey";

        static ILog log;
        static AzureServiceBusMessageQueue()
        {
            RebusLoggerFactory.Changed += f => log = f.GetCurrentClassLogger();
        }

        readonly ThreadLocal<Queue<BrokeredMessage>> messagesReceived = new ThreadLocal<Queue<BrokeredMessage>>(() => new Queue<BrokeredMessage>());
        readonly ThreadLocal<Dictionary<string, Queue<BrokeredMessage>>> messagesToSend = new ThreadLocal<Dictionary<string, Queue<BrokeredMessage>>>(() => new Dictionary<string, Queue<BrokeredMessage>>());
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
                        if (messagesToSend.Value != null)
                        {

                            foreach (KeyValuePair<string, Queue<BrokeredMessage>> queuePair in messagesToSend.Value)
                            {
                                var client = messagingFactory.CreateQueueClient(queuePair.Key);
                                var allMessages = queuePair.Value.ToArray();
                                client.SendBatch(allMessages);
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



            var receivedTransportMessage = new ReceivedTransportMessage
            {
                Id = Guid.NewGuid().ToString(),
                Headers = message.Headers,
                Body = message.Body,
                Label = message.Label

            };

            var brokeredMessage = new BrokeredMessage(receivedTransportMessage);
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
                EnqueueInMessageToSendDestinationQueue(destinationQueueName, brokeredMessage);

            }

        }

        void EnqueueInMessageToSendDestinationQueue(string destinationQueueName, BrokeredMessage brokeredMessage)
        {
        
            if (!messagesToSend.Value.ContainsKey(destinationQueueName))
            {
                messagesToSend.Value.Add(destinationQueueName, new Queue<BrokeredMessage>());

            }

            messagesToSend.Value[destinationQueueName].Enqueue(brokeredMessage);
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


                    var receivedTransportMessage = message.GetBody<ReceivedTransportMessage>();

                    if (receivedTransportMessage == null)
                    {
                        log.Warn("Received message with NULL data - how weird is that?");
                        message.Complete();
                        return null;
                    }

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

        public void Dispose()
        {
            Dispose(true);

        }
        public void Dispose(bool disposing)
        {
            if (disposing)
            {

                if (receiverQueueClient != null)
                {
                    receiverQueueClient.Close();
                }
                if (messagingFactory != null)
                {
                    messagingFactory.Close();
                }
            }



        }
        ~AzureServiceBusMessageQueue()
        {
            Dispose(false);
        }
    }
}