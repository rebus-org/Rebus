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
    public class AzureServiceBusMessageQueue : IDuplexTransport, IDisposable
    {
        static ILog log;

        static AzureServiceBusMessageQueue()
        {
            RebusLoggerFactory.Changed += f => log = f.GetCurrentClassLogger();
        }

        const string TopicName = "Rebus";
        const string LogicalQueuePropertyKey = "LogicalDestinationQueue";
        const string AzureServiceBusMessageBatch = "AzureServiceBusMessageBatch";
        const string AzureServiceBusReceivedMessage = "AzureServiceBusReceivedMessage";

        readonly NamespaceManager namespaceManager;
        readonly TopicDescription topicDescription;
        readonly TopicClient topicClient;
        readonly SubscriptionClient subscriptionClient;

        bool disposed;

        public AzureServiceBusMessageQueue(string connectionString, string inputQueue)
        {
            log.Info("Initializing Azure Service Bus transport with logical input queue '{0}'", inputQueue);

            namespaceManager = NamespaceManager.CreateFromConnectionString(connectionString);

            InputQueue = inputQueue;

            log.Info("Ensuring that topic '{0}' exists", TopicName);
            if (!namespaceManager.TopicExists(TopicName))
            {
                try
                {
                    namespaceManager.CreateTopic(TopicName);
                }
                catch
                {
                    // just assume the call failed because the topic already exists - if GetTopic below
                    // fails, then something must be wrong, and then we just want to fail immediately
                }
            }

            topicDescription = namespaceManager.GetTopic(TopicName);
            GetOrCreateSubscription(InputQueue);

            log.Info("Creating topic client");
            topicClient = TopicClient.CreateFromConnectionString(connectionString, topicDescription.Path);

            log.Info("Creating subscription client");
            subscriptionClient = SubscriptionClient.CreateFromConnectionString(connectionString, TopicName, InputQueue);
        }

        public void Send(string destinationQueueName, TransportMessageToSend message, ITransactionContext context)
        {
            if (!context.IsTransactional)
            {
                var envelopeToSendImmediately = new Envelope
                                                    {
                                                        Body = message.Body,
                                                        Headers = message.Headers != null
                                                                      ? message
                                                                            .Headers
                                                                            .ToDictionary(h => h.Key,
                                                                                          h => h.Value.ToString())
                                                                      : null,
                                                        Label = message.Label,
                                                    };

                var backoffTimes = new[] { 0.1, 0.2, 0.5, 1, 2, 3, 5, 8, 13, 21 }
                    .Select(TimeSpan.FromSeconds)
                    .ToArray();

                new Retrier(backoffTimes)
                    .RetryOn<ServerBusyException>()
                    .RetryOn<MessagingCommunicationException>()
                    .RetryOn<TimeoutException>()
                    .TolerateInnerExceptionsAsWell()
                    .Do(() =>
                        {
                            using (var messageToSendImmediately = new BrokeredMessage(envelopeToSendImmediately))
                            {
                                messageToSendImmediately.Properties[LogicalQueuePropertyKey] = destinationQueueName;
                                topicClient.Send(messageToSendImmediately);
                            }
                        });

                return;
            }

            if (context[AzureServiceBusMessageBatch] == null)
            {
                context[AzureServiceBusMessageBatch] = new List<BrokeredMessage>();
                context.DoCommit += () => DoCommit(context);
            }

            var envelope = new Envelope
                               {
                                   Body = message.Body,
                                   Headers = message.Headers != null
                                                 ? message
                                                       .Headers
                                                       .ToDictionary(h => h.Key,
                                                                     h => h.Value.ToString())
                                                 : null,
                                   Label = message.Label,
                               };

            var brokeredMessage = new BrokeredMessage(envelope);
            brokeredMessage.Properties[LogicalQueuePropertyKey] = destinationQueueName;

            ((List<BrokeredMessage>)context[AzureServiceBusMessageBatch]).Add(brokeredMessage);
        }

        public ReceivedTransportMessage ReceiveMessage(ITransactionContext context)
        {
            try
            {
                var brokeredMessage = subscriptionClient.Receive(TimeSpan.FromSeconds(1));

                if (brokeredMessage == null)
                {
                    return null;
                }

                var messageId = brokeredMessage.MessageId;

                try
                {
                    if (context.IsTransactional)
                    {
                        if (context[AzureServiceBusMessageBatch] != null)
                        {
                            throw new InvalidOperationException(
                                @"Attempted to receive message withing transaction where one or more messages were already sent - that cannot be done, sorry!");
                        }

                        context[AzureServiceBusReceivedMessage] = brokeredMessage;
                        context[AzureServiceBusMessageBatch] = new List<BrokeredMessage>();
                        context.DoCommit += () => DoCommit(context);
                        context.DoRollback += () =>
                            {
                                try
                                {
                                    brokeredMessage.Abandon();
                                }
                                catch (Exception e)
                                {
                                    log.Warn(
                                        "An exception occurred while attempting to abandon received message {0}: {1}",
                                        messageId, e);
                                }
                                finally
                                {
                                    brokeredMessage.Dispose();
                                }
                            };
                    }
                    else
                    {
                        brokeredMessage.Complete();
                    }

                    var envelope = brokeredMessage.GetBody<Envelope>();

                    return new ReceivedTransportMessage
                               {
                                   Id = messageId,
                                   Headers = envelope.Headers == null
                                                 ? new Dictionary<string, object>()
                                                 : envelope
                                                       .Headers
                                                       .ToDictionary(e => e.Key, e => (object)e.Value),
                                   Body = envelope.Body,
                                   Label = envelope.Label
                               };
                }
                catch (Exception receiveException)
                {
                    var message = string.Format("An exception occurred while handling brokered message {0}",
                                                messageId);

                    try
                    {
                        log.Info("Will attempt to abandon message {0}", messageId);
                        brokeredMessage.Abandon();
                    }
                    catch (Exception abandonException)
                    {
                        log.Warn("Got exception while abandoning message: {0}", abandonException);
                    }

                    throw new ApplicationException(message, receiveException);
                }
            }
            catch (TimeoutException)
            {
                return null;
            }
            catch (Exception e)
            {
                log.Warn("Caught exception while receiving message from logical queue '{0}': {1}", InputQueue, e);

                return null;
            }
        }

        void DoCommit(ITransactionContext context)
        {
            var receivedMessageOrNull = context[AzureServiceBusReceivedMessage] as BrokeredMessage;
            var brokeredMessagesToSend = (List<BrokeredMessage>)context[AzureServiceBusMessageBatch];

            try
            {
                var backoffTimes = new[] {0.1, 0.2, 0.5, 1, 2, 3, 5, 8, 13}
                    .Select(TimeSpan.FromSeconds)
                    .ToArray();

                new Retrier(backoffTimes)
                    .RetryOn<ServerBusyException>()
                    .RetryOn<MessagingCommunicationException>()
                    .RetryOn<TimeoutException>()
                    .TolerateInnerExceptionsAsWell()
                    .Do(() =>
                        {
                            using (var scope = new TransactionScope(TransactionScopeOption.RequiresNew))
                            {
                                if (brokeredMessagesToSend.Any())
                                {
                                    topicClient.SendBatch(brokeredMessagesToSend);
                                }

                                if (receivedMessageOrNull != null)
                                {
                                    receivedMessageOrNull.Complete();
                                }

                                scope.Complete();
                            }
                        });
            }
            catch (Exception e)
            {
                try
                {
                    if (receivedMessageOrNull != null)
                    {
                        receivedMessageOrNull.Abandon();
                    }
                }
                catch (Exception rollbackException)
                {
                    log.Warn("An exception occurred while attempting to roll back: {0}", rollbackException);
                }

                throw;
            }
            finally
            {
                try
                {
                    if (receivedMessageOrNull != null)
                    {
                        receivedMessageOrNull.Dispose();
                    }

                    brokeredMessagesToSend.ForEach(m => m.Dispose());
                }
                catch (Exception e)
                {
                    log.Warn("An exception occurred while disposing brokered messages: {0}", e);
                }
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

        class Retrier
        {
            readonly TimeSpan[] backoffTimes;
            readonly List<Type> toleratedExceptionTypes = new List<Type>();
            bool scanInnerExceptions;

            public Retrier(params TimeSpan[] backoffTimes)
            {
                this.backoffTimes = backoffTimes;
            }

            public Retrier TolerateInnerExceptionsAsWell()
            {
                scanInnerExceptions = true;
                return this;
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
                var caughtExceptions = new List<Timed<Exception>>();

                while (!complete)
                {
                    try
                    {
                        action();
                        complete = true;
                    }
                    catch (Exception e)
                    {
                        caughtExceptions.Add(e.At(DateTime.Now));

                        if (backoffIndex >= backoffTimes.Length)
                        {
                            throw;
                        }

                        if (ExceptionCanBeTolerated(e))
                        {
                            Thread.Sleep(backoffTimes[backoffIndex++]);
                        }
                        else
                        {
                            throw new AggregateException(string.Format("Operation did not complete within {0} retries which resulted in exceptions at the following times: {1}",
                                backoffTimes.Length, string.Join(", ", caughtExceptions.Select(c => c.Time))), caughtExceptions.Select(c => c.Value));
                        }
                    }
                }
            }

            bool ExceptionCanBeTolerated(Exception exceptionToCheck)
            {
                while (exceptionToCheck != null)
                {
                    var exceptionType = exceptionToCheck.GetType();

                    // if the exception can be tolerated...
                    if (toleratedExceptionTypes.Contains(exceptionType))
                    {
                        return true;
                    }

                    // otherwise, see if we are allowed to check the inner exception as well
                    exceptionToCheck = scanInnerExceptions
                        ? exceptionToCheck.InnerException
                        : null;
                }

                // exception cannot be tolerated
                return false;
            }
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

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposed) return;

            if (disposing)
            {
                try
                {
                    log.Info("Closing subscription client");
                    subscriptionClient.Close();
                }
                catch (Exception e)
                {
                    log.Warn("An exception occurred while closing the subscription client: {0}", e);
                }

                try
                {
                    log.Info("Closing topic client");
                    topicClient.Close();
                }
                catch (Exception e)
                {
                    log.Warn("An exception occurred while closing the topic client: {0}", e);
                }
            }

            disposed = true;
        }
    }
}