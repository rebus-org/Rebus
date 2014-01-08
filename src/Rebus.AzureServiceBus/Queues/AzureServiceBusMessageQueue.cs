using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Timers;
using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Messaging;
using Rebus.Logging;

namespace Rebus.AzureServiceBus.Queues
{
    public class AzureServiceBusMessageQueue : IDuplexTransport, IDisposable
    {
        static ILog log;

        static AzureServiceBusMessageQueue()
        {
            RebusLoggerFactory.Changed += f => log = f.GetCurrentClassLogger();
        }

        public const string AzureServiceBusRenewLeaseAction = "AzureServiceBusRenewLeaseAction (invoke in order to renew the peek lock on the current message)";

        const string AzureServiceBusMessageBatch = "AzureServiceBusMessageBatch";

        const string AzureServiceBusReceivedMessage = "AzureServiceBusReceivedMessage";
        
        const string AzureServiceBusReceivedMessageTime = "AzureServiceBusReceivedMessageTime";

        /// <summary>
        /// Will be used to cache queue clients for each queue that we need to communicate with
        /// </summary>
        readonly ConcurrentDictionary<string, QueueClient> queueClients = new ConcurrentDictionary<string, QueueClient>();
        readonly NamespaceManager namespaceManager;
        readonly string connectionString;

        bool disposed;

        /// <summary>
        /// Construct a send-only instance of the transport
        /// </summary>
        public static AzureServiceBusMessageQueue Sender(string connectionString)
        {
            return new AzureServiceBusMessageQueue(connectionString, null);
        }

        public AzureServiceBusMessageQueue(string connectionString, string inputQueue)
        {
            this.connectionString = connectionString;
            try
            {
                log.Info("Initializing Azure Service Bus transport with input queue '{0}'", inputQueue);

                namespaceManager = NamespaceManager.CreateFromConnectionString(connectionString);

                InputQueue = inputQueue;

                // if we're in one-way mode, just quit here
                if (inputQueue == null) return;

                EnsureQueueExists(inputQueue);
            }
            catch (Exception e)
            {
                throw new ApplicationException(
                    string.Format(
                        "An error occurred while initializing Azure Service Bus with input queue '{0}'",
                        inputQueue), e);
            }
        }

        void EnsureQueueExists(string queueName)
        {
            if (namespaceManager.QueueExists(queueName)) return;

            try
            {
                log.Info("Queue '{0}' does not exist - it will be created now", queueName);

                namespaceManager.CreateQueue(queueName);
            }
            catch
            {
                // just assume the call failed because the topic already exists - if GetTopic below
                // fails, then something must be wrong, and then we just want to fail immediately
            }
        }

        public void Send(string destinationQueueName, TransportMessageToSend message, ITransactionContext context)
        {
            if (!context.IsTransactional)
            {
                var envelopeToSendImmediately = CreateEnvelope(message);

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
                                GetClientFor(destinationQueueName).Send(messageToSendImmediately);
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

            var envelope = CreateEnvelope(message);

            var messagesToSend = (List<Tuple<string, Envelope>>)context[AzureServiceBusMessageBatch];

            messagesToSend.Add(Tuple.Create(destinationQueueName, envelope));
        }

        QueueClient GetClientFor(string destinationQueueName)
        {
            var client = queueClients.GetOrAdd(destinationQueueName, CreateNewClient);

            if (!client.IsClosed) return client;

            client = CreateNewClient(destinationQueueName);
            queueClients[destinationQueueName] = client;

            return client;
        }

        QueueClient CreateNewClient(string queueName)
        {
            return QueueClient.CreateFromConnectionString(connectionString, queueName);
        }

        public ReceivedTransportMessage ReceiveMessage(ITransactionContext context)
        {
            try
            {
                var brokeredMessage = GetClientFor(InputQueue).Receive(TimeSpan.FromSeconds(1));

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
                        context[AzureServiceBusReceivedMessageTime] = DateTime.UtcNow;
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

                        return CreateReceivedTransportMessage(messageId, envelope);
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
                    var message = string.Format("An exception occurred while handling brokered message {0}", messageId);

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
                    log.Warn("Caught exception while receiving message from queue '{0}': {1}", InputQueue, e);
                }

                return null;
            }
            catch (Exception e)
            {
                log.Warn("Caught exception while receiving message from queue '{0}': {1}", InputQueue, e);

                return null;
            }
        }

        Envelope CreateEnvelope(TransportMessageToSend message)
        {
            return new Envelope
            {
                Body = message.Body,
                Headers = message.Headers != null
                    ? message.Headers.ToDictionary(h => h.Key, h => h.Value.ToString())
                    : null,
                Label = message.Label,
            };
        }

        ReceivedTransportMessage CreateReceivedTransportMessage(string messageId, Envelope envelope)
        {
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
            var stuffToDispose = new List<IDisposable>();
            var receivedMessageOrNull = context[AzureServiceBusReceivedMessage] as BrokeredMessage;
            
            if (receivedMessageOrNull != null)
            {
                stuffToDispose.Add(receivedMessageOrNull);
            }

            var messagesToSend = (List<Tuple<string, Envelope>>)context[AzureServiceBusMessageBatch];

            try
            {
                var backoffTimes = new[] { 1, 2, 5, 10, 10, 10 }
                    .Select(seconds => TimeSpan.FromSeconds(seconds))
                    .ToArray();

                try
                {
                    if (messagesToSend.Any())
                    {
                        // if we have received a message, ensure that we keep the message alive for the duration of the commit phase
                        if (receivedMessageOrNull != null)
                        {
                            var renewLeaseTimer = new Timer();
                            stuffToDispose.Add(renewLeaseTimer);
                            renewLeaseTimer.Interval = 40000;
                            renewLeaseTimer.Elapsed += (o, ea) => RenewLease(context);
                            renewLeaseTimer.Start();

                            var receiveTime = (DateTime)context[AzureServiceBusReceivedMessageTime];

                            // if we've already spent a significant amount of time running the handler, make sure to renew the message right away
                            if (DateTime.UtcNow - receiveTime > TimeSpan.FromSeconds(15))
                            {
                                RenewLease(context);
                            }
                        }

                        var messagesForEachRecipient = messagesToSend
                            .GroupBy(g => g.Item1)
                            .ToList();

                        foreach (var group in messagesForEachRecipient)
                        {
                            var destinationQueueName = group.Key;
                            var messagesForThisRecipient = group.Select(g => g.Item2).ToList();

                            foreach (var batch in messagesForThisRecipient.Partition(100))
                            {
                                var brokeredMessages = batch
                                    .Select(envelope => new BrokeredMessage(envelope))
                                    .ToList();

                                stuffToDispose.AddRange(brokeredMessages);

                                new Retrier(backoffTimes)
                                    .RetryOn<ServerBusyException>()
                                    .RetryOn<MessagingCommunicationException>()
                                    .RetryOn<TimeoutException>()
                                    .TolerateInnerExceptionsAsWell()
                                    .Do(() => GetClientFor(destinationQueueName).SendBatch(brokeredMessages));
                            }
                        }
                    }

                    if (receivedMessageOrNull != null)
                    {
                        receivedMessageOrNull.Complete();
                    }
                }
                finally
                {
                    stuffToDispose.ForEach(d => d.Dispose());
                }
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
                if (receivedMessageOrNull != null)
                {
                    receivedMessageOrNull.Dispose();
                }
            }
        }

        void RenewLease(ITransactionContext context)
        {
            var renewLeaseAction = context[AzureServiceBusRenewLeaseAction] as Action;
            if (renewLeaseAction == null) return;

            try
            {
                renewLeaseAction();
            }
            catch (Exception exception)
            {
                log.Warn("An error occurred while attempting to auto-renew the lease during commit: {0}", exception);
            }
        }

        public string InputQueue { get; private set; }

        public string InputQueueAddress { get { return InputQueue; } }

        public AzureServiceBusMessageQueue Purge()
        {
            log.Warn("Purging queue '{0}'", InputQueue);

            namespaceManager.DeleteQueue(InputQueue);
            namespaceManager.CreateQueue(InputQueue);

            return this;
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
                    foreach (var client in queueClients)
                    {
                        log.Info("Closing queue client for '{0}'", client.Key);

                        client.Value.Close();
                    }
                }
                catch (Exception e)
                {
                    log.Warn("An exception occurred while closing queue client(s): {0}", e);
                }
            }

            disposed = true;
        }
    }
}