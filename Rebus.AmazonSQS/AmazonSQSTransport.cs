using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Amazon;
using Amazon.SQS;
using Amazon.SQS.Model;
using Rebus.Bus;
using Rebus.Extensions;
using Rebus.Logging;
using Rebus.Messages;
using Rebus.Threading;
using Rebus.Time;
using Rebus.Transport;
using Message = Amazon.SQS.Model.Message;
#pragma warning disable 1998

namespace Rebus.AmazonSQS
{
    /// <summary>
    /// Implementation of <see cref="ITransport"/> that uses Amazon Simple Queue Service to move messages around
    /// </summary>
    public class AmazonSqsTransport : ITransport, IInitializable
    {
        const string ClientContextKey = "SQS_Client";
        const string OutgoingMessagesItemsKey = "SQS_OutgoingMessages";

        readonly string _accessKeyId;
        readonly string _secretAccessKey;
        readonly RegionEndpoint _regionEndpoint;
        readonly IAsyncTaskFactory _asyncTaskFactory;
        readonly ILog _log;

        TimeSpan _peekLockDuration = TimeSpan.FromMinutes(5);
        TimeSpan _peekLockRenewalInterval = TimeSpan.FromMinutes(4);
        string _queueUrl;

        /// <summary>
        /// Constructs the transport with the specified settings
        /// </summary>
        public AmazonSqsTransport(string inputQueueAddress, string accessKeyId, string secretAccessKey, RegionEndpoint regionEndpoint, IRebusLoggerFactory rebusLoggerFactory, IAsyncTaskFactory asyncTaskFactory)
        {
            if (accessKeyId == null) throw new ArgumentNullException(nameof(accessKeyId));
            if (secretAccessKey == null) throw new ArgumentNullException(nameof(secretAccessKey));
            if (regionEndpoint == null) throw new ArgumentNullException(nameof(regionEndpoint));
            if (rebusLoggerFactory == null) throw new ArgumentNullException(nameof(rebusLoggerFactory));

            Address = inputQueueAddress;
            _log = rebusLoggerFactory.GetCurrentClassLogger();

            if (Address != null)
            {
                if (Address.Contains("/") && !Uri.IsWellFormedUriString(Address, UriKind.Absolute))
                {
                    throw new ArgumentException(
                        "You could either have a simple queue name without slash (eg. \"inputqueue\") - or a complete URL for the queue endpoint. (eg. \"https://sqs.eu-central-1.amazonaws.com/234234234234234/somqueue\")",
                        nameof(inputQueueAddress));
                }
            }

            _accessKeyId = accessKeyId;
            _secretAccessKey = secretAccessKey;
            _regionEndpoint = regionEndpoint;
            _asyncTaskFactory = asyncTaskFactory;
        }

        /// <summary>
        /// Public initialization method that allows for configuring the peek lock duration. Mostly useful for tests.
        /// </summary>
        public void Initialize(TimeSpan peeklockDuration)
        {
            _peekLockDuration = peeklockDuration;
            _peekLockRenewalInterval = TimeSpan.FromMinutes(_peekLockDuration.TotalMinutes * 0.8);

            CreateQueue(Address);
        }

        public void Initialize()
        {
            CreateQueue(Address);
        }


        public void CreateQueue(string address)
        {
            if (Address == null) return;

            _log.Info("Creating a new sqs queue:  with name: {0} on region: {1}", address, _regionEndpoint);

            using (var client = new AmazonSQSClient(_accessKeyId, _secretAccessKey, _regionEndpoint))
            {
                var queueName = GetQueueNameFromAddress(address);
                var response = client.CreateQueue(new CreateQueueRequest(queueName));

                if (response.HttpStatusCode != HttpStatusCode.OK)
                {
                    throw new Exception($"Could not create queue '{queueName}' - got HTTP {response.HttpStatusCode}");
                }

                _queueUrl = response.QueueUrl;
            }
        }

        /// <summary>
        /// Deletes all messages from the input queue
        /// </summary>
        public void Purge()
        {
            if (Address == null) return;

            using (var client = new AmazonSQSClient(_accessKeyId, _secretAccessKey, RegionEndpoint.EUCentral1))
            {
                try
                {
                    var response = client.ReceiveMessage(new ReceiveMessageRequest(_queueUrl)
                    {
                        MaxNumberOfMessages = 10
                    });

                    while (response.Messages.Any())
                    {
                        var deleteResponse = client.DeleteMessageBatch(_queueUrl, response.Messages
                        .Select(m => new DeleteMessageBatchRequestEntry(m.MessageId, m.ReceiptHandle))
                        .ToList());

                        if (!deleteResponse.Failed.Any())
                        {
                            response = client.ReceiveMessage(new ReceiveMessageRequest(_queueUrl)
                            {
                                MaxNumberOfMessages = 10
                            });
                        }
                        else
                        {
                            throw new Exception(deleteResponse.HttpStatusCode.ToString());
                        }
                    }


                }
                catch (Exception ex)
                {

                    Console.WriteLine("error in purge: " + ex.Message);
                }

            }





        }

        class OutgoingMessage
        {
            public string DestinationAddress { get; }
            public TransportMessage TransportMessage { get; }

            public OutgoingMessage(string destinationAddress, TransportMessage transportMessage)
            {
                DestinationAddress = destinationAddress;
                TransportMessage = transportMessage;
            }
        }

        public async Task Send(string destinationAddress, TransportMessage message, ITransactionContext context)
        {
            if (destinationAddress == null) throw new ArgumentNullException(nameof(destinationAddress));
            if (message == null) throw new ArgumentNullException(nameof(message));
            if (context == null) throw new ArgumentNullException(nameof(context));

            var outgoingMessages = context.GetOrAdd(OutgoingMessagesItemsKey, () =>
            {
                var sendMessageBatchRequestEntries = new ConcurrentQueue<OutgoingMessage>();

                context.OnCommitted(async () =>
                {
                    await SendOutgoingMessages(sendMessageBatchRequestEntries, context);
                });

                return sendMessageBatchRequestEntries;
            });
            
            outgoingMessages.Enqueue(new OutgoingMessage(destinationAddress, message));
        }

        async Task SendOutgoingMessages(ConcurrentQueue<OutgoingMessage> outgoingMessages, ITransactionContext context)
        {
            if (!outgoingMessages.Any()) return;

            var client = GetClientFromTransactionContext(context);

            var messagesByDestination = outgoingMessages
                .GroupBy(m => m.DestinationAddress)
                .ToList();

            await Task.WhenAll(
                messagesByDestination
                    .Select(async batch =>
                    {
                        var entries = batch
                            .Select(message =>
                            {
                                var transportMessage = message.TransportMessage;

                                var headers = transportMessage.Headers;

                                return new SendMessageBatchRequestEntry
                                {
                                    Id = headers[Headers.MessageId],
                                    MessageBody = GetBody(transportMessage.Body),
                                    MessageAttributes = CreateAttributesFromHeaders(headers),
                                    DelaySeconds = GetDelaySeconds(headers)
                                };
                            })
                            .ToList();

                        var destinationUrl = GetDestinationQueueUrlByName(batch.Key, context);

                        var request = new SendMessageBatchRequest(destinationUrl, entries);

                        var response = await client.SendMessageBatchAsync(request);

                        if (response.Failed.Any())
                        {
                            var failed = response.Failed.Select(f => new AmazonSQSException($"Failed {f.Message} with Id={f.Id}, Code={f.Code}, SenderFault={f.SenderFault}"));

                            throw new AggregateException(failed);
                        }
                    })

                );
        }

        static int GetDelaySeconds(Dictionary<string, string> headers)
        {
            string deferUntilTime;
            if (!headers.TryGetValue(Headers.DeferredUntil, out deferUntilTime)) return 0;

            var deferUntilDateTimeOffset = deferUntilTime.ToDateTimeOffset();

            var delay = (int)Math.Ceiling((deferUntilDateTimeOffset - RebusTime.Now).TotalSeconds);

            return delay;
        }

        public async Task<TransportMessage> Receive(ITransactionContext context)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));
            if (Address == null)
            {
                throw new InvalidOperationException("This Amazon SQS transport does not have an input queue, hence it is not possible to reveive anything");
            }

            var client = GetClientFromTransactionContext(context);

            var request = new ReceiveMessageRequest(_queueUrl)
            {
                MaxNumberOfMessages = 1,
                WaitTimeSeconds = 1,
                AttributeNames = new List<string>(new[] { "All" }),
                MessageAttributeNames = new List<string>(new[] { "All" })
            };

            var response = await client.ReceiveMessageAsync(request);

            if (!response.Messages.Any()) return null;

            var message = response.Messages.First();

            var renewalTask = CreateRenewalTaskForMessage(message, client);

            context.OnCompleted(async () =>
            {
                renewalTask.Dispose();

                await client.DeleteMessageAsync(new DeleteMessageRequest(_queueUrl, message.ReceiptHandle));
            });

            context.OnAborted(() =>
            {
                renewalTask.Dispose();

                client.ChangeMessageVisibility(_queueUrl, message.ReceiptHandle, 0);
            });

            if (MessageIsExpired(message))
            {
                await client.DeleteMessageAsync(new DeleteMessageRequest(_queueUrl, message.ReceiptHandle));
                return null;
            }
            renewalTask.Start();
            var transportMessage = GetTransportMessage(message);
            return transportMessage;
        }

        IAsyncTask CreateRenewalTaskForMessage(Message message, AmazonSQSClient client)
        {
            return _asyncTaskFactory.Create($"RenewPeekLock-{message.MessageId}",
                async () =>
                {
                    _log.Info("Renewing peek lock for message with ID {0}", message.MessageId);

                    await
                        client.ChangeMessageVisibilityAsync(new ChangeMessageVisibilityRequest(_queueUrl,
                            message.ReceiptHandle, (int) _peekLockDuration.TotalSeconds));
                },
                intervalSeconds: (int) _peekLockRenewalInterval.TotalSeconds,
                prettyInsignificant: true);
        }

        static bool MessageIsExpired(Message message)
        {
            MessageAttributeValue value;
            if (!message.MessageAttributes.TryGetValue(Headers.TimeToBeReceived, out value))
                return false;

            var timeToBeReceived = TimeSpan.Parse(value.StringValue);

            if (MessageIsExpiredUsingRebusSentTime(message, timeToBeReceived)) return true;
            if (MessageIsExpiredUsingNativeSqsSentTimestamp(message, timeToBeReceived)) return true;

            return false;
        }

        static bool MessageIsExpiredUsingRebusSentTime(Message message, TimeSpan timeToBeReceived)
        {
            MessageAttributeValue rebusUtcTimeSentAttributeValue;
            if (message.MessageAttributes.TryGetValue(Headers.SentTime, out rebusUtcTimeSentAttributeValue))
            {
                var rebusUtcTimeSent = DateTimeOffset.ParseExact(rebusUtcTimeSentAttributeValue.StringValue, "O", null);

                if (RebusTime.Now.UtcDateTime - rebusUtcTimeSent > timeToBeReceived)
                {
                    return true;
                }

            }

            return false;

        }

        static bool MessageIsExpiredUsingNativeSqsSentTimestamp(Message message, TimeSpan timeToBeReceived)
        {
            string sentTimeStampString;
            if (message.Attributes.TryGetValue("SentTimestamp", out sentTimeStampString))
            {
                var sentTime = GetTimeFromUnixTimestamp(sentTimeStampString);
                if (RebusTime.Now.UtcDateTime - sentTime > timeToBeReceived)
                {
                    return true;
                }
            }
            return false;
        }

        private static DateTime GetTimeFromUnixTimestamp(string sentTimeStampString)
        {
            var unixTime = long.Parse(sentTimeStampString);
            var epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var sentTime = epoch.AddMilliseconds(unixTime);
            return sentTime;
        }

        private AmazonSQSClient GetClientFromTransactionContext(ITransactionContext context)
        {
            return context.GetOrAdd(ClientContextKey, () =>
            {
                var amazonSqsClient = new AmazonSQSClient(_accessKeyId, _secretAccessKey, new AmazonSQSConfig()
                {
                    RegionEndpoint = _regionEndpoint
                });
                context.OnDisposed(amazonSqsClient.Dispose);
                return amazonSqsClient;
            });
        }

        private TransportMessage GetTransportMessage(Message message)
        {
            var headers = message.MessageAttributes.ToDictionary(kv => kv.Key, kv => kv.Value.StringValue);

            return new TransportMessage(headers, GetBodyBytes(message.Body));

        }
        private string GetBody(byte[] bodyBytes)
        {
            return Convert.ToBase64String(bodyBytes);
        }

        private byte[] GetBodyBytes(string bodyText)
        {
            return Convert.FromBase64String(bodyText);
        }
        private Dictionary<string, MessageAttributeValue> CreateAttributesFromHeaders(Dictionary<string, string> headers)
        {
            return headers.ToDictionary(key => key.Key,
                                        value => new MessageAttributeValue { DataType = "String", StringValue = value.Value });
        }
        private readonly ConcurrentDictionary<string, string> _queueUrls = new ConcurrentDictionary<string, string>();

        private string GetDestinationQueueUrlByName(string address, ITransactionContext transactionContext)
        {
            var url = _queueUrls.GetOrAdd("DestinationAddress" + address.ToLowerInvariant(), key =>
                    {
                        if (Uri.IsWellFormedUriString(address, UriKind.Absolute))
                        {
                            return address;
                        }

                        _log.Info("Getting queueUrl from SQS service by name:{0}", address);
                        var client = GetClientFromTransactionContext(transactionContext);
                        var urlResponse = client.GetQueueUrl(address);

                        if (urlResponse.HttpStatusCode == HttpStatusCode.OK)
                        {
                            return urlResponse.QueueUrl;
                        }

                        _log.Warn("Could not get queue url from service by name: {0}", address);
                        throw new ApplicationException($"could not find Url for address: {address} - got errorcode: {urlResponse.HttpStatusCode}");
                    });

            return url;

        }
        private string GetQueueNameFromAddress(string address)
        {
            if (Uri.IsWellFormedUriString(address, UriKind.Absolute))
            {
                var queueFullAddress = new Uri(address);

                return queueFullAddress.Segments[queueFullAddress.Segments.Length - 1];
            }

            return address;

        }

        public string Address { get; }

        public void DeleteQueue()
        {
            using (var client = new AmazonSQSClient(_accessKeyId, _secretAccessKey, _regionEndpoint))
            {
                client.DeleteQueue(_queueUrl);
            }
        }
    }
}
