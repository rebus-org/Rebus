using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Messaging;
using Rebus.Bus;
using Rebus.Extensions;
using Rebus.Logging;
using Rebus.Messages;
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
        readonly QueueClient _inputQueueClient;

        readonly TimeSpan _peekLockDuration = TimeSpan.FromMinutes(5);
        readonly TimeSpan _peekLockRenewalInterval = TimeSpan.FromMinutes(4);

        public AzureServiceBusTransport(string connectionString, string inputQueueAddress)
        {
            _namespaceManager = NamespaceManager.CreateFromConnectionString(connectionString);
            _inputQueueClient = QueueClient.CreateFromConnectionString(connectionString, inputQueueAddress, ReceiveMode.PeekLock);
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
            var brokeredMessage = new BrokeredMessage(message.Body);

            foreach (var kvp in message.Headers)
            {
                brokeredMessage.Properties[kvp.Key] = kvp.Value;
            }

            context.Committed += () => GetQueueClient(destinationAddress).Send(brokeredMessage);
            context.Cleanup += () => brokeredMessage.Dispose();
        }

        public async Task<TransportMessage> Receive(ITransactionContext context)
        {
            var brokeredMessage = await _inputQueueClient.ReceiveAsync(TimeSpan.FromSeconds(1));

            if (brokeredMessage == null) return null;

            var headers = brokeredMessage.Properties
                .Where(kvp => kvp.Value is string)
                .ToDictionary(kvp => kvp.Key, kvp => (string) kvp.Value);

            var messageId = headers.GetValue(Headers.MessageId);

            _log.Debug("Received brokered message with ID {0}", messageId);

            var cancellationTokenSource = new CancellationTokenSource();
            var cancellationToken = cancellationTokenSource.Token;

            var task = Task.Factory.StartNew(async () =>
            {
                while (true)
                {
                    _log.Info("WAITING");
                    await Task.Delay(_peekLockRenewalInterval, cancellationToken);

                    if (cancellationToken.IsCancellationRequested)
                    {
                        _log.Info("CANCELLED  ___ WOOT");
                        break;
                    }

                    _log.Info("Renewing peek lock for message with ID {0}", messageId);
                    await brokeredMessage.RenewLockAsync();
                }
            }, cancellationToken);

            context.Items
                .GetOrAdd("asb-transport-peek-lock-renewers", () => new List<Task>())
                .Add(task);

            context.Aborted += () =>
            {
                _log.Debug("Abandoning message with ID {0}", messageId);
                brokeredMessage.Abandon();
            };
            context.Committed += () =>
            {
                _log.Debug("Completing message with ID {0}", messageId);
                brokeredMessage.Complete();
            };
            context.Cleanup += () =>
            {
                cancellationTokenSource.Cancel();    

                _log.Debug("Disposing message with ID {0}", messageId);
                brokeredMessage.Dispose();
            };

            return new TransportMessage(headers, brokeredMessage.GetBody<Stream>());
        }

        QueueClient GetQueueClient(string queueAddress)
        {
            return _queueClients.GetOrAdd(queueAddress, address => QueueClient.CreateFromConnectionString(_connectionString, address));
        }

        public string Address
        {
            get { return _inputQueueAddress; }
        }
    }
}
