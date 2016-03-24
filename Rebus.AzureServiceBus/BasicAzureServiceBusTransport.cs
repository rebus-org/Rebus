using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Messaging;
using Rebus.Bus;
using Rebus.Exceptions;
using Rebus.Extensions;
using Rebus.Logging;
using Rebus.Messages;
using Rebus.Threading;
using Rebus.Transport;
#pragma warning disable 1998

namespace Rebus.AzureServiceBus
{
    public class BasicAzureServiceBusTransport : ITransport, IInitializable
    {
        const string OutgoingMessagesKey = "azure-service-bus-transport";

        static readonly TimeSpan[] RetryWaitTimes =
        {
            TimeSpan.FromSeconds(0.1),
            TimeSpan.FromSeconds(0.1),
            TimeSpan.FromSeconds(0.1),
            TimeSpan.FromSeconds(0.2),
            TimeSpan.FromSeconds(0.2),
            TimeSpan.FromSeconds(0.2),
            TimeSpan.FromSeconds(0.5),
            TimeSpan.FromSeconds(1),
            TimeSpan.FromSeconds(1),
            TimeSpan.FromSeconds(1),
            TimeSpan.FromSeconds(5),
            TimeSpan.FromSeconds(5),
            TimeSpan.FromSeconds(10),
        };

        readonly ConcurrentDictionary<string, QueueClient> _queueClients = new ConcurrentDictionary<string, QueueClient>(StringComparer.InvariantCultureIgnoreCase);
        readonly NamespaceManager _namespaceManager;
        readonly string _connectionString;
        readonly IRebusLoggerFactory _rebusLoggerFactory;
        readonly IAsyncTaskFactory _asyncTaskFactory;
        protected readonly string InputQueueAddress;

        readonly TimeSpan _peekLockDuration = TimeSpan.FromMinutes(5);
        readonly AsyncBottleneck _bottleneck = new AsyncBottleneck(10);
        readonly Ignorant _ignorant = new Ignorant();
        protected readonly ILog Log;

        readonly ConcurrentQueue<BrokeredMessage> _prefetchQueue = new ConcurrentQueue<BrokeredMessage>();

        bool _prefetchingEnabled;
        int _numberOfMessagesToPrefetch;

        /// <summary>
        /// Constructs the transport, connecting to the service bus pointed to by the connection string.
        /// </summary>
        public BasicAzureServiceBusTransport(string connectionString, string inputQueueAddress, IRebusLoggerFactory rebusLoggerFactory, IAsyncTaskFactory asyncTaskFactory)
        {
            if (connectionString == null) throw new ArgumentNullException("connectionString");
            
            _namespaceManager = NamespaceManager.CreateFromConnectionString(connectionString);
            _connectionString = connectionString;
            _rebusLoggerFactory = rebusLoggerFactory;
            _asyncTaskFactory = asyncTaskFactory;
            Log = rebusLoggerFactory.GetCurrentClassLogger();

            if (inputQueueAddress != null)
            {
                InputQueueAddress = inputQueueAddress.ToLowerInvariant();
            }
        }

        public virtual void Initialize()
        {
            Log.Info("Initializing Azure Service Bus transport with queue '{0}'", InputQueueAddress);

            if (InputQueueAddress != null)
            {
                CreateQueue(InputQueueAddress);
            }
        }

        /// <summary>
        /// Purges the input queue by deleting it and creating it again
        /// </summary>
        public void PurgeInputQueue()
        {
            if (InputQueueAddress == null) return;

            Log.Info("Purging queue '{0}'", InputQueueAddress);
            _namespaceManager.DeleteQueue(InputQueueAddress);

            CreateQueue(InputQueueAddress);
        }

        /// <summary>
        /// Configures the transport to prefetch the specified number of messages into an in-mem queue for processing, disabling automatic peek lock renewal
        /// </summary>
        public void PrefetchMessages(int numberOfMessagesToPrefetch)
        {
            _prefetchingEnabled = true;
            _numberOfMessagesToPrefetch = numberOfMessagesToPrefetch;
        }

        /// <summary>
        /// Enables automatic peek lock renewal - only recommended if you truly need to handle messages for a very long time
        /// </summary>
        public bool AutomaticallyRenewPeekLock { get; set; }

        public void CreateQueue(string address)
        {
            if (_namespaceManager.QueueExists(address)) return;

            var queueDescription = new QueueDescription(address)
            {
                MaxSizeInMegabytes = 1024,
                MaxDeliveryCount = 100,
                LockDuration = _peekLockDuration,
                EnablePartitioning = PartitioningEnabled
            };

            try
            {
                Log.Info("Input queue '{0}' does not exist - will create it now", InputQueueAddress);
                _namespaceManager.CreateQueue(queueDescription);
                Log.Info("Created!");
            }
            catch (MessagingEntityAlreadyExistsException)
            {
                // fair enough...
                Log.Info("MessagingEntityAlreadyExistsException - carrying on");
            }
        }

        public virtual async Task Send(string destinationAddress, TransportMessage message, ITransactionContext context)
        {
            GetOutgoingMessages(context)
                .GetOrAdd(destinationAddress, _ => new ConcurrentQueue<TransportMessage>())
                .Enqueue(message);
        }

        static BrokeredMessage CreateBrokeredMessage(TransportMessage message)
        {
            var headers = message.Headers;
            var brokeredMessage = new BrokeredMessage(new MemoryStream(message.Body), true);

            foreach (var kvp in headers)
            {
                brokeredMessage.Properties[kvp.Key] = PossiblyLimitLength(kvp.Value);
            }

            if (headers.ContainsKey(Headers.TimeToBeReceived))
            {
                var timeToBeReceivedStr = headers[Headers.TimeToBeReceived];
                var timeToBeReceived = TimeSpan.Parse(timeToBeReceivedStr);
                brokeredMessage.TimeToLive = timeToBeReceived;
            }

            string contentType;
            if (headers.TryGetValue(Headers.ContentType, out contentType))
            {
                brokeredMessage.ContentType = contentType;
            }

            string correlationId;
            if (headers.TryGetValue(Headers.CorrelationId, out correlationId))
            {
                brokeredMessage.CorrelationId = correlationId;
            }

            brokeredMessage.Label = message.GetMessageLabel();

            return brokeredMessage;
        }

        static string PossiblyLimitLength(string str)
        {
            const int maxLengthPrettySafe = 16300;

            if (str.Length < maxLengthPrettySafe) return str;

            return string.Format("{0} (... cut out because length exceeded {1} characters ...) {2}",
                str.Substring(0, 8000),
                maxLengthPrettySafe,
                str.Substring(str.Length - 8000));
        }

        /// <summary>
        /// Should return a new <see cref="Retrier"/>, fully configured to correctly "accept" the right exceptions
        /// </summary>
        static Retrier GetRetrier()
        {
            return new Retrier(RetryWaitTimes)
                .On<MessagingException>(e => e.IsTransient)
                .On<MessagingCommunicationException>(e => e.IsTransient)
                .On<ServerBusyException>(e => e.IsTransient);
        }

        public async Task<TransportMessage> Receive(ITransactionContext context)
        {
            if (InputQueueAddress == null)
            {
                throw new InvalidOperationException("This Azure Service Bus transport does not have an input queue, hence it is not possible to reveive anything");
            }

            using (await _bottleneck.Enter())
            {
                var brokeredMessage = await ReceiveBrokeredMessage();

                if (brokeredMessage == null) return null;

                var headers = brokeredMessage.Properties
                    .Where(kvp => kvp.Value is string)
                    .ToDictionary(kvp => kvp.Key, kvp => (string)kvp.Value);

                var messageId = headers.GetValueOrNull(Headers.MessageId);
                var leaseDuration = (brokeredMessage.LockedUntilUtc - DateTime.UtcNow);
                var lockRenewalInterval = TimeSpan.FromMinutes(0.8 * leaseDuration.TotalMinutes);

                var renewalTask = GetRenewalTaskOrFakeDisposable(messageId, brokeredMessage, lockRenewalInterval);

                context.OnAborted(() =>
                {
                    renewalTask.Dispose();

                    try
                    {
                        brokeredMessage.Abandon();
                    }
                    catch (Exception exception)
                    {
                        // if it fails, it'll be back on the queue anyway....
                        Log.Warn("Could not abandon message: {0}", exception);
                    }
                });

                context.OnCommitted(async () => renewalTask.Dispose());

                context.OnCompleted(async () =>
                {
                    await brokeredMessage.CompleteAsync();
                });

                context.OnDisposed(() =>
                {
                    renewalTask.Dispose();
                    brokeredMessage.Dispose();
                });

                using (var memoryStream = new MemoryStream())
                {
                    await brokeredMessage.GetBody<Stream>().CopyToAsync(memoryStream);
                    return new TransportMessage(headers, memoryStream.ToArray());
                }
            }
        }

        ConcurrentDictionary<string, ConcurrentQueue<TransportMessage>> GetOutgoingMessages(ITransactionContext context)
        {
            return context.GetOrAdd(OutgoingMessagesKey, () =>
            {
                var destinations = new ConcurrentDictionary<string, ConcurrentQueue<TransportMessage>>();

                context.OnCommitted(async () =>
                {
                    // send outgoing messages
                    foreach (var destinationAndMessages in destinations)
                    {
                        var destinationAddress = destinationAndMessages.Key;
                        var messages = destinationAndMessages.Value;

                        var sendTasks = messages
                            .Select(async message =>
                            {
                                await GetRetrier().Execute(async () =>
                                {
                                    using (var brokeredMessageToSend = CreateBrokeredMessage(message))
                                    {
                                        try
                                        {
                                            await GetQueueClient(destinationAddress).SendAsync(brokeredMessageToSend);
                                        }
                                        catch (MessagingEntityNotFoundException exception)
                                        {
                                            // do NOT rethrow as MessagingEntityNotFoundException because it has its own ToString that swallows most of the info!!
                                            throw new MessagingException($"Could not send to '{destinationAddress}'!", false, exception);
                                        }
                                    }
                                });
                            })
                            .ToArray();

                        await Task.WhenAll(sendTasks);
                    }
                });

                return destinations;
            });
        }

        IDisposable GetRenewalTaskOrFakeDisposable(string messageId, BrokeredMessage brokeredMessage, TimeSpan lockRenewalInterval)
        {
            if (AutomaticallyRenewPeekLock)
            {
                var renewalTask = _asyncTaskFactory.Create($"RenewPeekLock-{messageId}",
                    async () =>
                    {
                        Log.Info("Renewing peek lock for message with ID {0}", messageId);

                        await brokeredMessage.RenewLockAsync();
                    },
                    intervalSeconds: (int) lockRenewalInterval.TotalSeconds,
                    prettyInsignificant: true);

                renewalTask.Start();

                return renewalTask;
            }

            return new FakeDisposable();
        }

        class FakeDisposable : IDisposable
        {
            public void Dispose()
            {
            }
        }

        async Task<BrokeredMessage> ReceiveBrokeredMessage()
        {
            var queueAddress = InputQueueAddress;

            if (_prefetchingEnabled)
            {
                BrokeredMessage nextMessage;

                if (_prefetchQueue.TryDequeue(out nextMessage))
                {
                    return nextMessage;
                }

                var client = GetQueueClient(queueAddress);

                var brokeredMessages = (await client.ReceiveBatchAsync(_numberOfMessagesToPrefetch, TimeSpan.FromSeconds(1))).ToList();

                _ignorant.Reset();

                if (!brokeredMessages.Any()) return null; 

                foreach (var receivedMessage in brokeredMessages)
                {
                    _prefetchQueue.Enqueue(receivedMessage);
                }

                _prefetchQueue.TryDequeue(out nextMessage);

                return nextMessage; //< just accept null at this point if there was nothing
            }

            try
            {
                var brokeredMessage = await GetQueueClient(queueAddress).ReceiveAsync(TimeSpan.FromSeconds(1));

                _ignorant.Reset();

                return brokeredMessage;
            }
            catch (Exception exception)
            {
                if (_ignorant.IsToBeIgnored(exception)) return null;

                QueueClient possiblyFaultyQueueClient;

                if (_queueClients.TryRemove(queueAddress, out possiblyFaultyQueueClient))
                {
                    CloseQueueClient(possiblyFaultyQueueClient);
                }

                throw;
            }
        }

        static void CloseQueueClient(QueueClient queueClientToClose)
        {
            try
            {
                queueClientToClose.Close();
            }
            catch (Exception)
            {
                // ignored because we don't care!
            }
        }

        QueueClient GetQueueClient(string queueAddress)
        {
            var queueClient = _queueClients.GetOrAdd(queueAddress, address =>
            {
                Log.Debug("Initializing new queue client for {0}", address);

                var newQueueClient = QueueClient.CreateFromConnectionString(_connectionString, address, ReceiveMode.PeekLock);

                return newQueueClient;
            });

            return queueClient;
        }

        public string Address
        {
            get { return InputQueueAddress; }
        }

        public bool PartitioningEnabled { get; set; }

        public void Dispose()
        {
            DisposePrefetchedMessages();

            _queueClients.Values.ForEach(CloseQueueClient);
        }

        void DisposePrefetchedMessages()
        {
            BrokeredMessage brokeredMessage;
            while (_prefetchQueue.TryDequeue(out brokeredMessage))
            {
                using (brokeredMessage)
                {
                    try
                    {
                        brokeredMessage.Abandon();
                    }
                    catch (Exception exception)
                    {
                        Log.Warn("Could not abandon brokered message with ID {0}: {1}", brokeredMessage.MessageId, exception);
                    }
                }
            }
        }         
    }

}