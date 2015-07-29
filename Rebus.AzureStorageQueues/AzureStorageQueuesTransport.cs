using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Queue;
using Microsoft.WindowsAzure.Storage.RetryPolicies;
using Newtonsoft.Json;
using Rebus.Bus;
using Rebus.Exceptions;
using Rebus.Logging;
using Rebus.Messages;
using Rebus.Transport;

#pragma warning disable 1998

namespace Rebus.AzureStorageQueues
{
    /// <summary>
    /// Implementation of <see cref="ITransport"/> that uses Azure Storage Queues to do its thing
    /// </summary>
    public class AzureStorageQueuesTransport : ITransport, IInitializable
    {
        static ILog _log;

        static AzureStorageQueuesTransport()
        {
            RebusLoggerFactory.Changed += f => _log = f.GetCurrentClassLogger();
        }

        readonly ConcurrentDictionary<string, CloudQueue> _queues = new ConcurrentDictionary<string, CloudQueue>();
        readonly TimeSpan _initialVisibilityDelay = TimeSpan.FromMinutes(5);
        readonly CloudQueueClient _queueClient;
        readonly string _inputQueueName;

        /// <summary>
        /// Constructs the transport
        /// </summary>
        public AzureStorageQueuesTransport(CloudStorageAccount storageAccount, string inputQueueName)
        {
            if (storageAccount == null) throw new ArgumentNullException("storageAccount");
            if (inputQueueName == null) throw new ArgumentNullException("inputQueueName");

            _inputQueueName = inputQueueName.ToLowerInvariant();
            _queueClient = storageAccount.CreateCloudQueueClient();
        }

        /// <summary>
        /// Creates a new queue with the specified address
        /// </summary>
        public void CreateQueue(string address)
        {
            var queue = GetQueue(address);

            queue.CreateIfNotExists();
        }

        /// <summary>
        /// Sends the given <see cref="TransportMessage"/> to the queue with the specified globally addressable name
        /// </summary>
        public async Task Send(string destinationAddress, TransportMessage message, ITransactionContext context)
        {
            context.OnCommitted(async () =>
            {
                var queue = GetQueue(destinationAddress);
                var messageId = Guid.NewGuid().ToString();
                var popReceipt = Guid.NewGuid().ToString();
                var timeToBeReceived = GetTimeToBeReceivedOrNull(message);
                var cloudQueueMessage = Serialize(message, messageId, popReceipt);

                try
                {
                    var options = new QueueRequestOptions {RetryPolicy = new ExponentialRetry()};
                    var operationContext = new OperationContext();

                    await queue.AddMessageAsync(cloudQueueMessage, timeToBeReceived, null, options, operationContext);
                }
                catch (Exception exception)
                {
                    throw new RebusApplicationException(string.Format("Could not send message with ID {0} to '{1}'", cloudQueueMessage.Id, destinationAddress), exception);
                }
            });
        }

        /// <summary>
        /// Receives the next message (if any) from the transport's input queue <see cref="ITransport.Address"/>
        /// </summary>
        public async Task<TransportMessage> Receive(ITransactionContext context)
        {
            var inputQueue = GetQueue(_inputQueueName);

            var cloudQueueMessage = await inputQueue.GetMessageAsync(_initialVisibilityDelay, new QueueRequestOptions(), new OperationContext());

            if (cloudQueueMessage == null) return null;

            context.OnCompleted(async () =>
            {
                await inputQueue.DeleteMessageAsync(cloudQueueMessage);
            });

            context.OnAborted(() =>
            {
                inputQueue.UpdateMessage(cloudQueueMessage, TimeSpan.FromSeconds(0), MessageUpdateFields.Visibility);
            });

            return Deserialize(cloudQueueMessage);
        }

        static TimeSpan? GetTimeToBeReceivedOrNull(TransportMessage message)
        {
            var headers = message.Headers;
            TimeSpan? timeToBeReceived = null;
            if (headers.ContainsKey(Headers.TimeToBeReceived))
            {
                var timeToBeReceivedStr = headers[Headers.TimeToBeReceived];
                timeToBeReceived = TimeSpan.Parse(timeToBeReceivedStr);
            }
            return timeToBeReceived;
        }

        static CloudQueueMessage Serialize(TransportMessage message, string messageId, string popReceipt)
        {
            var cloudStorageQueueTransportMessage = new CloudStorageQueueTransportMessage
            {
                Headers = message.Headers,
                Body = message.Body
            };

            var cloudQueueMessage = new CloudQueueMessage(messageId, popReceipt);
            cloudQueueMessage.SetMessageContent(JsonConvert.SerializeObject(cloudStorageQueueTransportMessage));
            return cloudQueueMessage;
        }

        static TransportMessage Deserialize(CloudQueueMessage cloudQueueMessage)
        {
            var cloudStorageQueueTransportMessage = JsonConvert.DeserializeObject<CloudStorageQueueTransportMessage>(cloudQueueMessage.AsString);

            return new TransportMessage(cloudStorageQueueTransportMessage.Headers, cloudStorageQueueTransportMessage.Body);
        }

        class CloudStorageQueueTransportMessage
        {
            public Dictionary<string, string> Headers { get; set; }
            public byte[] Body { get; set; }
        }

        public string Address
        {
            get { return _inputQueueName; }
        }

        public void Initialize()
        {
            CreateQueue(_inputQueueName);
        }

        CloudQueue GetQueue(string address)
        {
            return _queues.GetOrAdd(address, _ => _queueClient.GetQueueReference(address));
        }

        /// <summary>
        /// Purges the input queue (WARNING: potentially very slow operation, as it will continue to batch receive messages until the queue is empty
        /// </summary>
        /// <exception cref="RebusApplicationException"></exception>
        public void PurgeInputQueue()
        {
            var queue = GetQueue(_inputQueueName);

            if (!queue.Exists()) return;

            _log.Info("Purging storage queue '{0}' (purging by deleting all messages)", _inputQueueName);

            try
            {
                while (true)
                {
                    var messages = queue.GetMessages(10).ToList();

                    if (!messages.Any()) break;

                    Task.WaitAll(messages.Select(async message =>
                    {
                        await queue.DeleteMessageAsync(message);
                    }).ToArray());

                    _log.Debug("Deleted {0} messages from '{1}'", messages.Count, _inputQueueName);
                }
            }
            catch (Exception exception)
            {
                throw new RebusApplicationException("Could not purge queue", exception);
            }
        }
    }
}
