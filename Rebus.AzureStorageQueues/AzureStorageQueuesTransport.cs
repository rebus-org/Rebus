using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Queue;
using Newtonsoft.Json;
using Rebus.Bus;
using Rebus.Logging;
using Rebus.Messages;
using Rebus.Transport;

namespace Rebus.AzureStorageQueues
{
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

        public AzureStorageQueuesTransport(CloudStorageAccount storageAccount, string inputQueueName)
        {
            if (storageAccount == null) throw new ArgumentNullException("storageAccount");
            if (inputQueueName == null) throw new ArgumentNullException("inputQueueName");

            _inputQueueName = inputQueueName.ToLowerInvariant();
            _queueClient = storageAccount.CreateCloudQueueClient();
        }

        public void CreateQueue(string address)
        {
            var queue = GetQueue(address);

            queue.CreateIfNotExists();
        }

        public async Task Send(string destinationAddress, TransportMessage message, ITransactionContext context)
        {
            context.OnCommitted(async () =>
            {
                var queue = GetQueue(destinationAddress);

                var messageId = Guid.NewGuid().ToString();
                var popReceipt = Guid.NewGuid().ToString();

                var cloudQueueMessage = Serialize(message, messageId, popReceipt);

                //var headers = message.Headers;
                //TimeSpan? timeToBeReceived = null;
                //if (headers.ContainsKey(Headers.TimeToBeReceived))
                //{
                //    var timeToBeReceivedStr = headers[Headers.TimeToBeReceived];
                //    timeToBeReceived = TimeSpan.Parse(timeToBeReceivedStr);
                //}

                try
                {
                    await queue.AddMessageAsync(cloudQueueMessage);
                }
                catch (Exception exception)
                {
                    throw new ApplicationException(string.Format("Could not send message with ID {0} to '{1}'", cloudQueueMessage.Id, destinationAddress), exception);
                }
            });
        }

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
                throw new ApplicationException("Could not purge queue", exception);
            }
        }
    }
}
