using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Messaging;
using Rebus.Bus;
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

        public void CreateQueue(string address)
        {
            if (_namespaceManager.QueueExists(address)) return;

            var queueDescription = new QueueDescription(address)
            {
                MaxSizeInMegabytes = 1024,
                MaxDeliveryCount = 100,
                LockDuration = TimeSpan.FromMinutes(5),
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

            _log.Debug("Received message with ID {0}", brokeredMessage.MessageId);

            context.Aborted += () =>
            {
                _log.Debug("Abandoning message with ID {0}", brokeredMessage.MessageId);
                brokeredMessage.Abandon();
            };
            context.Committed += () =>
            {
                _log.Debug("Completing message with ID {0}", brokeredMessage.MessageId);
                brokeredMessage.Complete();
            };
            context.Cleanup += () =>
            {
                _log.Debug("Disposing brokered message with ID {0}", brokeredMessage.MessageId);
                brokeredMessage.Dispose();
            };

            var headers = brokeredMessage.Properties
                .Where(kvp => kvp.Value is string)
                .ToDictionary(kvp => kvp.Key, kvp => (string) kvp.Value);

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
