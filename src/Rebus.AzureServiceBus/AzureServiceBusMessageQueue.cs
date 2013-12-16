using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.ServiceModel;
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

        public const string TopicName = "Rebus";
        public const string LogicalQueuePropertyKey = "LogicalDestinationQueue";

        public const string AzureServiceBusRenewLeaseAction = "AzureServiceBusRenewLeaseAction (invoke in order to renew the peek lock on the current message)";

        const string AzureServiceBusMessageBatch = "AzureServiceBusMessageBatch";

        const string AzureServiceBusReceivedMessage = "AzureServiceBusReceivedMessage";

        readonly NamespaceManager namespaceManager;

        readonly TopicDescription topicDescription;

        readonly TopicClient topicClient;

        readonly SubscriptionClient subscriptionClient;

        bool disposed;

        public static AzureServiceBusMessageQueue Sender(string connectionString)
        {
            return new AzureServiceBusMessageQueue(connectionString, null);
        }

        public AzureServiceBusMessageQueue(string connectionString, string inputQueue)
        {
            try
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

                log.Info("Creating topic client");
                topicClient = TopicClient.CreateFromConnectionString(connectionString, topicDescription.Path);

                // if we're in one-way mode, just quit here
                if (inputQueue == null) return;

                GetOrCreateSubscription(InputQueue);

                log.Info("Creating subscription client");
                subscriptionClient = SubscriptionClient.CreateFromConnectionString(connectionString, TopicName, InputQueue, ReceiveMode.PeekLock);
            }
            catch (Exception e)
            {
                throw new ApplicationException(
                    string.Format(
                        "An error occurred while initializing Azure Service Bus with logical input queue '{0}'",
                        inputQueue), e);
            }
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

                var backoffTimes = new[] { 1, 2, 5, 10, 10, 10, 10, 10, 20, 20, 20, 30, 30, 30, 30 }
                    .Select(seconds => TimeSpan.FromSeconds(seconds))
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

            // if the batch is null, we're doing tx send outside of a message handler
            if (context[AzureServiceBusMessageBatch] == null)
            {
                context[AzureServiceBusMessageBatch] = new List<Tuple<string, Envelope>>();
                context.DoCommit += () => DoCommit(context);
            }

            var envelope = new Envelope
            {
                Body = message.Body,
                Headers = message.Headers != null
                    ? message.Headers.ToDictionary(h => h.Key, h => h.Value.ToString())
                    : null,
                Label = message.Label,
            };

            var messagesToSend = (List<Tuple<string, Envelope>>)context[AzureServiceBusMessageBatch];

            messagesToSend.Add(Tuple.Create(destinationQueueName, envelope));

            if (messagesToSend.Count > 100)
            {
                var errorMessage = string.Format("Cannot send more than 100 messages in one transaction with Azure Service Bus." +
                                                 " This is a limitation in the service bus that you must handle in the way you're" +
                                                 " using Rebus with Azure Service Bus, possibly by introducting a way of batching" +
                                                 " work in another way.");

                throw new InvalidOperationException(errorMessage);
            }

            if (messagesToSend.Count > 90)
            {
                log.Warn("Currently carrying {0} messages that will be sent with the batch API when the transaction is" +
                         " committed - this is pretty close to 100 which the absolute maximum number of messages that" +
                         " can be sent in one single transaction with Azure Service Bus. At the moment, there's no other" +
                         " workaround than handling this in the way you're using Rebus with Azure Service Bus.", messagesToSend.Count);
            }
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
                                @"Attempted to receive message within transaction where one or more messages were already sent - that cannot be done, sorry!");
                        }

                        context[AzureServiceBusReceivedMessage] = brokeredMessage;
                        context[AzureServiceBusMessageBatch] = new List<Tuple<string, Envelope>>();

                        // inject method into message context to allow for long-running message handling operations to have their lock renewed
                        context[AzureServiceBusRenewLeaseAction] = (Action)(() =>
                        {
                            try
                            {
                                var messageToRenew = (BrokeredMessage)context[AzureServiceBusReceivedMessage];

                                log.Info("Renewing lock on message {0}", messageId);

                                messageToRenew.RenewLock();
                            }
                            catch (Exception exception)
                            {
                                throw new ApplicationException(
                                    string.Format(
                                        "An error occurred while attempting to renew the lock on message {0}", messageId),
                                    exception);
                            }
                        });
                        context.DoCommit += () => DoCommit(context);
                        context.DoRollback += () => DoRollBack(context);
                        context.Cleanup += () => DoCleanUp(context);
                    }

                    try
                    {
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
                    finally
                    {
                        if (!context.IsTransactional)
                        {
                            brokeredMessage.Complete();
                            brokeredMessage.Dispose();
                        }
                    }
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
            catch (CommunicationObjectFaultedException)
            {
                return null;
            }
            catch (MessagingCommunicationException e)
            {
                if (!e.IsTransient)
                {
                    log.Warn("Caught exception while receiving message from logical queue '{0}': {1}", InputQueue, e);
                }

                return null;
            }
            catch (Exception e)
            {
                log.Warn("Caught exception while receiving message from logical queue '{0}': {1}", InputQueue, e);

                return null;
            }
        }

        void DoRollBack(ITransactionContext context)
        {
            try
            {
                var brokeredMessage = (BrokeredMessage)context[AzureServiceBusReceivedMessage];

                brokeredMessage.Abandon();
            }
            catch
            {
            }

        }
        void DoCleanUp(ITransactionContext context)
        {
            try
            {
                var brokeredMessage = (BrokeredMessage)context[AzureServiceBusReceivedMessage];

                brokeredMessage.Dispose();
            }
            catch
            {
            }

        }

        void DoCommit(ITransactionContext context)
        {
            // the message will be null when doing tx send outside of a message handler
            var receivedMessageOrNull = context[AzureServiceBusReceivedMessage] as BrokeredMessage;
            var messagesToSend = (List<Tuple<string, Envelope>>)context[AzureServiceBusMessageBatch];

            try
            {
                var backoffTimes = new[] { 1, 2, 5, 10, 10, 10 }
                    .Select(seconds => TimeSpan.FromSeconds(seconds))
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
                                var brokeredMessagesToSend = new List<BrokeredMessage>();

                                if (messagesToSend.Any())
                                {
                                    brokeredMessagesToSend.AddRange(messagesToSend
                                        .Select(tuple =>
                                        {
                                            var brokeredMessage = new BrokeredMessage(tuple.Item2);
                                            brokeredMessage.Properties[LogicalQueuePropertyKey] = tuple.Item1;
                                            return brokeredMessage;
                                        }));

                                    topicClient.SendBatch(brokeredMessagesToSend);
                                }

                                if (receivedMessageOrNull != null)
                                {
                                    receivedMessageOrNull.Complete();
                                }

                                scope.Complete();

                                try
                                {
                                    brokeredMessagesToSend.ForEach(m => m.Dispose());
                                }
                                catch{}
                            }
                        });
            }
            catch (Exception)
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

        public void GetOrCreateSubscription(string logicalQueueName)
        {
            if (namespaceManager.SubscriptionExists(topicDescription.Path, logicalQueueName)) return;

            try
            {
                log.Info("Establishing subscription '{0}'", logicalQueueName);
                var filter = new SqlFilter(string.Format("LogicalDestinationQueue='{0}'", logicalQueueName));

                var newSubscription = namespaceManager.CreateSubscription(topicDescription.Path, logicalQueueName, filter);

                // effectively disable Azure Service Bus' own dead lettering
                newSubscription.MaxDeliveryCount = 1000;
                newSubscription.EnableDeadLetteringOnMessageExpiration = false;
                newSubscription.LockDuration = TimeSpan.FromMinutes(5);
                newSubscription.UserMetadata = string.Format("Created by Rebus: {0}", DateTime.UtcNow.ToString("u"));

                namespaceManager.UpdateSubscription(newSubscription);
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
                    if (subscriptionClient != null)
                    {
                        log.Info("Closing subscription client");
                        subscriptionClient.Close();
                    }
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