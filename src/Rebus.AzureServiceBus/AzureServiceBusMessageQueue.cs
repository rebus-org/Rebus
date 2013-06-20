using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Threading;
using System.Transactions;
using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Messaging;
using System.Linq;
using Rebus.Logging;

namespace Rebus.AzureServiceBus
{
    public class AzureServiceBusMessageQueue : IDuplexTransport
    {
        static ILog log;

        static AzureServiceBusMessageQueue()
        {
            RebusLoggerFactory.Changed += f => log = f.GetCurrentClassLogger();
        }

        const string TopicName = "Rebus";
        const string LogicalQueuePropertyKey = "LogicalDestinationQueue";

        readonly NamespaceManager namespaceManager;
        readonly TopicDescription topicDescription;
        readonly TopicClient topicClient;
        readonly SubscriptionClient subscriptionClient;

        public AzureServiceBusMessageQueue(string connectionString, string inputQueue)
        {
            log.Info("Initializing Azure Service Bus transport with logical input queue '{0}'", inputQueue);

            namespaceManager = NamespaceManager.CreateFromConnectionString(connectionString);

            InputQueue = inputQueue;

            try
            {
                log.Info("Ensuring that topic '{0}' exists", TopicName);
                namespaceManager.CreateTopic(TopicName);
            }
            catch { }

            topicDescription = namespaceManager.GetTopic(TopicName);
            GetOrCreateSubscription(InputQueue);

            log.Info("Creating topic client");
            topicClient = TopicClient.CreateFromConnectionString(connectionString, topicDescription.Path);

            log.Info("Creating subscription client");
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

            // if we're transactional, let the transaction happen and do its thing
            if (context.IsTransactional)
            {
                topicClient.Send(brokeredMessage);
                return;
            }

            new Retrier(TimeSpan.FromSeconds(.1),
                        TimeSpan.FromSeconds(.2),
                        TimeSpan.FromSeconds(.5),
                        TimeSpan.FromSeconds(1),
                        TimeSpan.FromSeconds(2))
                .RetryOn<ServerBusyException>()
                .RetryOn<MessagingCommunicationException>()
                .RetryOn<TimeoutException>()
                .Do(() => topicClient.Send(brokeredMessage));
        }

        class Retrier
        {
            readonly TimeSpan[] backoffs;
            readonly List<Type> toleratedExceptionTypes = new List<Type>();

            public Retrier(params TimeSpan[] backoffs)
            {
                this.backoffs = backoffs;
            }

            public Retrier RetryOn<TException>() where TException : Exception
            {
                toleratedExceptionTypes.Add(typeof(TException));
                return this;
            }

            public void Do(Action action)
            {
                var backoffIndex = 0;
                var complete = false;
                
                while (!complete)
                {
                    try
                    {
                        action();
                        complete = true;
                    }
                    catch (Exception e)
                    {
                        if (backoffIndex >= backoffs.Length)
                        {
                            throw;
                        }

                        if (toleratedExceptionTypes.Contains(e.GetType()))
                        {
                            Thread.Sleep(backoffs[backoffIndex++]);
                        }
                        else
                        {
                            throw;
                        }
                    }
                }
            }
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
                                                   .ToDictionary(e => e.Key, e => (object)e.Value),
                               Body = envelope.Body,
                               Label = envelope.Label
                           };
            }
            catch (Exception e)
            {
                log.Warn("Caught exception while receiving message: {0}", e);
                return null;
            }
        }

        public string InputQueue { get; private set; }

        public string InputQueueAddress { get { return InputQueue; } }

        public AzureServiceBusMessageQueue Purge()
        {
            log.Warn("Purging logical queue {0}", InputQueue);

            namespaceManager.DeleteSubscription(topicDescription.Path, InputQueue);
            GetOrCreateSubscription(InputQueue);
            
            return this;
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

        void GetOrCreateSubscription(string logicalQueueName)
        {
            if (namespaceManager.SubscriptionExists(topicDescription.Path, logicalQueueName)) return;

            try
            {
                log.Info("Establishing subscription '{0}'", logicalQueueName);
                var filter = new SqlFilter(string.Format("LogicalDestinationQueue = '{0}'", logicalQueueName));
                namespaceManager.CreateSubscription(topicDescription.Path, logicalQueueName, filter);
            }
            catch (MessagingEntityAlreadyExistsException)
            {
            }
        }
    }
}