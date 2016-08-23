using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Queue;
using Microsoft.WindowsAzure.Storage.RetryPolicies;
using Newtonsoft.Json;
using Rebus.Bus;
using Rebus.Exceptions;
using Rebus.Extensions;
using Rebus.Logging;
using Rebus.Messages;
using Rebus.Time;
using Rebus.Transport;

#pragma warning disable 1998

namespace Rebus.AzureStorage.Transport
{
    /// <summary>
    /// Implementation of <see cref="ITransport"/> that uses Azure Storage Queues to do its thing
    /// </summary>
    public class AzureStorageQueuesTransport : ITransport, IInitializable
    {
        readonly ConcurrentDictionary<string, CloudQueue> _queues = new ConcurrentDictionary<string, CloudQueue>();
        readonly TimeSpan _initialVisibilityDelay = TimeSpan.FromMinutes(5);
        readonly CloudQueueClient _queueClient;
        readonly string _inputQueueName;
        readonly ILog _log;

        /// <summary>
        /// Constructs the transport
        /// </summary>
        public AzureStorageQueuesTransport(CloudStorageAccount storageAccount, string inputQueueName, IRebusLoggerFactory rebusLoggerFactory)
        {
            if (storageAccount == null) throw new ArgumentNullException(nameof(storageAccount));

            _queueClient = storageAccount.CreateCloudQueueClient();
            _log = rebusLoggerFactory.GetCurrentClassLogger();

            if (inputQueueName != null)
            {
                _inputQueueName = inputQueueName.ToLowerInvariant();
            }
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
                var headers = message.Headers.Clone();
                var queue = GetQueue(destinationAddress);
                var messageId = Guid.NewGuid().ToString();
                var popReceipt = Guid.NewGuid().ToString();
                var timeToBeReceivedOrNull = GetTimeToBeReceivedOrNull(headers);
                var queueVisibilityDelayOrNull = GetQueueVisibilityDelayOrNull(headers);
                var cloudQueueMessage = Serialize(messageId, popReceipt, headers, message.Body);

                try
                {
                    var options = new QueueRequestOptions {RetryPolicy = new ExponentialRetry()};
                    var operationContext = new OperationContext();

                    await queue.AddMessageAsync(cloudQueueMessage, timeToBeReceivedOrNull, queueVisibilityDelayOrNull, options, operationContext);
                }
                catch (Exception exception)
                {
                    throw new RebusApplicationException(exception, $"Could not send message with ID {cloudQueueMessage.Id} to '{destinationAddress}'");
                }
            });
        }

        /// <summary>
        /// Receives the next message (if any) from the transport's input queue <see cref="ITransport.Address"/>
        /// </summary>
        public async Task<TransportMessage> Receive(ITransactionContext context, CancellationToken cancellationToken)
        {
            if (_inputQueueName == null)
            {
                throw new InvalidOperationException("This Azure Storage Queues transport does not have an input queue, hence it is not possible to receive anything");
            }
            var inputQueue = GetQueue(_inputQueueName);

            var cloudQueueMessage = await inputQueue.GetMessageAsync(_initialVisibilityDelay, new QueueRequestOptions(), new OperationContext(), cancellationToken);

            if (cloudQueueMessage == null) return null;

            context.OnCompleted(async () =>
            {
                // if we get this far, don't pass on the cancellation token
                // ReSharper disable once MethodSupportsCancellation
                await inputQueue.DeleteMessageAsync(cloudQueueMessage);
            });

            context.OnAborted(() =>
            {
                inputQueue.UpdateMessage(cloudQueueMessage, TimeSpan.FromSeconds(0), MessageUpdateFields.Visibility);
            });

            return Deserialize(cloudQueueMessage);
        }

        static TimeSpan? GetTimeToBeReceivedOrNull(Dictionary<string, string> headers)
        {
            string timeToBeReceivedStr;

            if (!headers.TryGetValue(Headers.TimeToBeReceived, out timeToBeReceivedStr))
            {
                return null;
            }
            
            TimeSpan? timeToBeReceived = TimeSpan.Parse(timeToBeReceivedStr);
            return timeToBeReceived;
        }

        internal static TimeSpan? GetQueueVisibilityDelayOrNull(Dictionary<string, string> headers)
        {
            string deferredUntilDateTimeOffsetString;

            if (!headers.TryGetValue(Headers.DeferredUntil, out deferredUntilDateTimeOffsetString))
            {
                return null;
            }

            headers.Remove(Headers.DeferredUntil);

            var enqueueTime = deferredUntilDateTimeOffsetString.ToDateTimeOffset();

            var difference = enqueueTime - RebusTime.Now;
            if (difference <= TimeSpan.Zero) return null;
            return difference;
        }

        static CloudQueueMessage Serialize(string messageId, string popReceipt, Dictionary<string, string> headers, byte[] body)
        {
            var cloudStorageQueueTransportMessage = new CloudStorageQueueTransportMessage
            {
                Headers = headers,
                Body = body
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

        public string Address => _inputQueueName;

        public void Initialize()
        {
            if (_inputQueueName != null)
            {
                _log.Info("Initializing Azure Storage Queues transport with queue '{0}'", _inputQueueName);
                CreateQueue(_inputQueueName);
                return;
            }

            _log.Info("Initializing one-way Azure Storage Queues transport");
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
                throw new RebusApplicationException(exception, "Could not purge queue");
            }
        }
    }
}
