using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Transactions;
using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Messaging;
using System.Linq;

namespace Rebus.AzureServiceBus
{
    public class AzureServiceBusMessageQueue : IDuplexTransport
    {
        const string TopicName = "Rebus";
        const string LogicalQueuePropertyKey = "LogicalDestinationQueue";
        
        readonly NamespaceManager namespaceManager;
        readonly TopicDescription topicDescription;
        readonly TopicClient topicClient;
        readonly SubscriptionClient subscriptionClient;

        public AzureServiceBusMessageQueue(string connectionString, string inputQueue)
        {
            namespaceManager = NamespaceManager.CreateFromConnectionString(connectionString);
            
            InputQueue = inputQueue;

            try
            {
                namespaceManager.CreateTopic(TopicName);
            }
            catch{}

            topicDescription = namespaceManager.GetTopic(TopicName);
            GetOrCreateSubscription(topicDescription, InputQueue);
            topicClient = TopicClient.CreateFromConnectionString(connectionString, topicDescription.Path);
            subscriptionClient = SubscriptionClient.CreateFromConnectionString(connectionString, TopicName, InputQueue);
        }

        public void Send(string destinationQueueName, TransportMessageToSend message, ITransactionContext context)
        {
            var envelope = new Envelope
                               {
                                   Body = message.Body,
                                   Headers = message.Headers != null
                                                 ? message
                                                       .Headers
                                                       .ToDictionary(h => h.Key, h => h.Value.ToString())
                                                 : null,
                                   Label = message.Label,
                               };

            if (context.IsTransactional && Transaction.Current == null)
            {
                var transaction = new TransactionScope();
                context.DoCommit += transaction.Complete;
            }

            var brokeredMessage = new BrokeredMessage(envelope);
            brokeredMessage.Properties[LogicalQueuePropertyKey] = destinationQueueName;
            topicClient.Send(brokeredMessage);
        }

        public ReceivedTransportMessage ReceiveMessage(ITransactionContext context)
        {
            try
            {
                var brokeredMessage = subscriptionClient.Receive(TimeSpan.FromSeconds(1));

                if (brokeredMessage == null)
                    return null;

                var envelope = brokeredMessage.GetBody<Envelope>();

                if (context.IsTransactional)
                {
                    context.DoCommit += brokeredMessage.Complete;
                    context.DoRollback += brokeredMessage.Abandon;
                }
                else
                {
                    brokeredMessage.Complete();
                }

                return new ReceivedTransportMessage
                           {
                               Id = brokeredMessage.MessageId,
                               Headers = envelope.Headers == null
                                             ? new Dictionary<string, object>()
                                             : envelope
                                                   .Headers
                                                   .ToDictionary(e => e.Key, e => (object) e.Value),
                               Body = envelope.Body,
                               Label = envelope.Label
                           };
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return null;
            }
        }

        public string InputQueue { get; private set; }

        public string InputQueueAddress { get { return InputQueue; } }

        public AzureServiceBusMessageQueue Purge()
        {
            namespaceManager.DeleteSubscription(topicDescription.Path, InputQueue);
            GetOrCreateSubscription(topicDescription, InputQueue);
            return this;
        }

        [DataContract]
        class Envelope
        {
            [DataMember]
            public Dictionary<string,string> Headers { get; set; }

            [DataMember]
            public byte[] Body { get; set; }

            [DataMember]
            public string Label { get; set; }
        }

        void GetOrCreateSubscription(TopicDescription topicDescription, string name)
        {
            if (namespaceManager.SubscriptionExists(topicDescription.Path, name)) return;
            
            try
            {
                var filter = new SqlFilter(string.Format("LogicalDestinationQueue = '{0}'", name));
                namespaceManager.CreateSubscription(topicDescription.Path, name, filter);
            }
            catch (MessagingEntityAlreadyExistsException)
            {
            }
        }
    }
}