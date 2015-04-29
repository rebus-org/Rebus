using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Messaging;
using Rebus.Bus;
using Rebus.Extensions;
using Rebus.Logging;
using Rebus.Messages;
using Rebus.Threading;
using Rebus.Transport;

namespace Rebus.AzureServiceBus
{
    public class AzureServiceBusTransport : ITransport, IInitializable
    {
        static ILog _log;

        static AzureServiceBusTransport()
        {
            RebusLoggerFactory.Changed += f => _log = f.GetCurrentClassLogger();
        }

        readonly ConcurrentDictionary<string, QueueClient> _queueClients = new ConcurrentDictionary<string, QueueClient>(StringComparer.InvariantCultureIgnoreCase);
        readonly NamespaceManager _namespaceManager;
        readonly string _connectionString;
        readonly string _inputQueueAddress;

        readonly TimeSpan _peekLockDuration = TimeSpan.FromMinutes(5);
        readonly TimeSpan _peekLockRenewalInterval = TimeSpan.FromMinutes(4);
        readonly AsyncBottleneck _bottleneck = new AsyncBottleneck(10);

        public AzureServiceBusTransport(string connectionString, string inputQueueAddress)
        {
            _namespaceManager = NamespaceManager.CreateFromConnectionString(connectionString);
            _connectionString = connectionString;
            _inputQueueAddress = inputQueueAddress;
        }

        public void Initialize()
        {
            _log.Info("Initializing Azure Service Bus transport with queue '{0}'", _inputQueueAddress);

            CreateQueue(_inputQueueAddress);
        }

        public void PurgeInputQueue()
        {
            _log.Info("Purging queue '{0}'", _inputQueueAddress);
            _namespaceManager.DeleteQueue(_inputQueueAddress);

            CreateQueue(_inputQueueAddress);
        }

        public void CreateQueue(string address)
        {
            if (_namespaceManager.QueueExists(address)) return;

            var queueDescription = new QueueDescription(address)
            {
                MaxSizeInMegabytes = 1024,
                MaxDeliveryCount = 100,
                LockDuration = _peekLockDuration,
            };

            try
            {
                _log.Info("Input queue '{0}' does not exist - will create it now", _inputQueueAddress);
                _namespaceManager.CreateQueue(queueDescription);
                _log.Info("Created!");
            }
            catch (MessagingEntityAlreadyExistsException)
            {
                // fair enough...
                _log.Info("MessagingEntityAlreadyExistsException - carrying on");
            }
        }

        public async Task Send(string destinationAddress, TransportMessage message, ITransactionContext context)
        {
            var headers = message.Headers;
            var body = message.Body;

            var brokeredMessage = new BrokeredMessage(body);

            foreach (var kvp in headers)
            {
                brokeredMessage.Properties[kvp.Key] = kvp.Value;
            }

            if (headers.ContainsKey(Headers.TimeToBeReceived))
            {
                var timeToBeReceivedStr = headers[Headers.TimeToBeReceived];
                var timeToBeReceived = TimeSpan.Parse(timeToBeReceivedStr);
                brokeredMessage.TimeToLive = timeToBeReceived;
            }

            context.OnCommitted(async () =>
            {
                await GetQueueClient(destinationAddress).SendAsync(brokeredMessage);
            });

            context.OnDisposed(() => brokeredMessage.Dispose());
        }

        public async Task<TransportMessage> Receive(ITransactionContext context)
        {
            using (await _bottleneck.Enter())
            {
                var brokeredMessage = await GetQueueClient(_inputQueueAddress).ReceiveAsync(TimeSpan.FromSeconds(1));

                if (brokeredMessage == null) return null;

                var headers = brokeredMessage.Properties
                    .Where(kvp => kvp.Value is string)
                    .ToDictionary(kvp => kvp.Key, kvp => (string)kvp.Value);

                var messageId = headers.GetValueOrNull(Headers.MessageId);

                _log.Debug("Received brokered message with ID {0}", messageId);

                var renewalTask = new AsyncTask(string.Format("RenewPeekLock-{0}", messageId),
                    async () =>
                    {
                        _log.Info("Renewing peek lock for message with ID {0}", messageId);

                        await brokeredMessage.RenewLockAsync();
                    })
                {
                    Interval = _peekLockRenewalInterval
                };

                renewalTask.Start();

                context.OnAborted(() =>
                {
                    renewalTask.Dispose();

                    _log.Debug("Abandoning message with ID {0}", messageId);
                    brokeredMessage.Abandon();
                });

                context.OnCommitted(async () =>
                {
                    renewalTask.Dispose();

                    _log.Debug("Completing message with ID {0}", messageId);
                    await brokeredMessage.CompleteAsync();
                });

                context.OnDisposed(() =>
                {
                    renewalTask.Dispose();

                    _log.Debug("Disposing message with ID {0}", messageId);
                    brokeredMessage.Dispose();
                });

                return new TransportMessage(headers, brokeredMessage.GetBody<byte[]>());
            }
        }

        QueueClient GetQueueClient(string queueAddress)
        {
            var queueClient = _queueClients.GetOrAdd(queueAddress, address =>
            {
                _log.Debug("Initializing new queue client for {0}", address);

                var newQueueClient = QueueClient.CreateFromConnectionString(_connectionString, address, ReceiveMode.PeekLock);

                return newQueueClient;
            });

            return queueClient;
        }

        public string Address
        {
            get { return _inputQueueAddress; }
        }
    }
}
