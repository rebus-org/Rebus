using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.Serialization;
using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Messaging;
using Rebus.Logging;
using System.Linq;

namespace Rebus.AzureServiceBus
{
    public class AzureServiceBusMessageQueue3 : IDuplexTransport, IDisposable
    {
        static ILog log;

        static AzureServiceBusMessageQueue3()
        {
            RebusLoggerFactory.Changed += f => log = f.GetCurrentClassLogger();
        }

        readonly string connectionString;
        readonly ConcurrentDictionary<string, QueueClient> outputQueueClients = new ConcurrentDictionary<string, QueueClient>();
        readonly QueueClient inputQueueClient;
        

        public AzureServiceBusMessageQueue3(string connectionString, string inputQueueName)
        {
            this.connectionString = connectionString;
            InputQueue = inputQueueName;

            var namespaceManager = NamespaceManager.CreateFromConnectionString(connectionString);

            if (!namespaceManager.QueueExists(inputQueueName))
            {
                try
                {
                    namespaceManager.CreateQueue(inputQueueName);
                }
                catch (MessagingEntityAlreadyExistsException) { }
            }

            inputQueueClient = QueueClient.CreateFromConnectionString(this.connectionString, inputQueueName);
        }

        public void Send(string destinationQueueName, TransportMessageToSend message, ITransactionContext context)
        {
            var key = destinationQueueName.ToLowerInvariant();
            var client = outputQueueClients.GetOrAdd(key, keyToAdd => QueueClient.CreateFromConnectionString(connectionString, destinationQueueName));

            var envelope = new Envelope
                               {
                                   Headers = message
                                       .Headers
                                       .ToDictionary(k => k.Key, v => v.Value.ToString()),
                                   Body = message.Body,
                                   Label = message.Label,
                               };
            var brokeredMessage = new BrokeredMessage(envelope);

            if (context.IsTransactional){}
        }

        public ReceivedTransportMessage ReceiveMessage(ITransactionContext context)
        {
            return new ReceivedTransportMessage();
        }

        [DataContract]
        class Envelope
        {
            [DataMember]
            public Dictionary<string, string> Headers { get; set; }

            [DataMember]
            public byte[] Body { get; set; }

            [DataMember]
            public string Label { get; set; }
        }

        public string InputQueue { get; private set; }

        public string InputQueueAddress { get { return InputQueue; } }

        public void Dispose()
        {
            throw new NotImplementedException();
        }
    }
}