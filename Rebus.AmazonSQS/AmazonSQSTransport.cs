using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Amazon;
using Amazon.SQS;
using Amazon.SQS.Model;
using Rebus.AmazonSQS.Config;
using Rebus.Bus;
using Rebus.Extensions;
using Rebus.Logging;
using Rebus.Messages;
using Rebus.Threading;
using Rebus.Transport;
using Message = Amazon.SQS.Model.Message;

namespace Rebus.AmazonSQS
{
    /// <summary>
    /// Implementation of <see cref="ITransport"/> that uses Amazon Simple Queue Service to move messages around
    /// </summary>
    public class AmazonSqsTransport : ITransport, IInitializable
    {
        static ILog _log;
        static AmazonSqsTransport()
        {
            RebusLoggerFactory.Changed += f => _log = f.GetCurrentClassLogger();
        }
        private TimeSpan _peekLockDuration = TimeSpan.FromMinutes(5);
        private TimeSpan _peekLockRenewalInterval = TimeSpan.FromMinutes(4);
        private readonly string _inputQueueAddress;
        private readonly string _accessKeyId;
        private readonly string _secretAccessKey;

        private readonly RegionEndpoint _regionEndpoint;
        private const string ClientContextKey = "SQS_Client";
        private const string OutgoingQueueContextKey = "SQS_outgoingQueue";
        private const string OutgoingQueueContextActionIsSetKey = "SQS_OutgoingQueueContextActionIsSet";
        private string _queueUrl;

        /// <summary>
        /// Constructs the transport with the specified settings
        /// </summary>
        public AmazonSqsTransport(string inputQueueAddress, string accessKeyId, string secretAccessKey, RegionEndpoint regionEndpoint)
        {
            if (inputQueueAddress == null) throw new ArgumentNullException("inputQueueAddress");
            if (accessKeyId == null) throw new ArgumentNullException("accessKeyId");
            if (secretAccessKey == null) throw new ArgumentNullException("secretAccessKey");

            if (regionEndpoint == null) throw new ArgumentNullException("regionEndpoint");
            _inputQueueAddress = inputQueueAddress;
            if (_inputQueueAddress.Contains("/") && !Uri.IsWellFormedUriString(_inputQueueAddress, UriKind.Absolute))
            {
                throw new ArgumentException("You could either have a simple queue name without slash (eg. \"inputqueue\") - or a complete URL for the queue endpoint. (eg. \"https://sqs.eu-central-1.amazonaws.com/234234234234234/somqueue\")", "inputQueueAddress");
            }

            _accessKeyId = accessKeyId;
            _secretAccessKey = secretAccessKey;
            _regionEndpoint = regionEndpoint;

        }

        /// <summary>
        /// Public initialization method that allows for configuring the peek lock duration. Mostly useful for tests.
        /// </summary>
        public void Initialize(TimeSpan peeklockDuration)
        {
            _peekLockDuration = peeklockDuration;

            _peekLockRenewalInterval = TimeSpan.FromMinutes(_peekLockDuration.TotalMinutes * 0.8);
            CreateQueue(_inputQueueAddress);
        }

        public void Initialize()
        {
            CreateQueue(_inputQueueAddress);
        }


        public void CreateQueue(string address)
        {
            _log.Info("Creating a new sqs queue:  with name: {0} on region: {1}", address, _regionEndpoint);


            using (var client = new AmazonSQSClient(_accessKeyId, _secretAccessKey, _regionEndpoint))
            {
                var queueName = GetQueueNameFromAddress(address);
                var response = client.CreateQueue(new CreateQueueRequest(queueName));

                if (response.HttpStatusCode != HttpStatusCode.OK)
                {

                    _log.Error("Did not create queue with address: {0} - there was an error: ErrorCode: {1}", address, response.HttpStatusCode);
                    //Not really sure when status code != ok - because sqlclient always throwsexceptions...
                }
                _queueUrl = response.QueueUrl;
            }
        }

        /// <summary>
        /// Deletes all messages from the input queue
        /// </summary>
        public void Purge()
        {
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
        public Task Send(string destinationAddress, TransportMessage message, ITransactionContext context)
        {
            if (destinationAddress == null) throw new ArgumentNullException("destinationAddress");
            if (message == null) throw new ArgumentNullException("message");
            if (context == null) throw new ArgumentNullException("context");

            var outputQueue = context.GetOrAdd(OutgoingQueueContextKey, () => new InMemOutputQueue());
            var contextActionsSet = context.GetOrAdd(OutgoingQueueContextActionIsSetKey, () => false);

            if (!contextActionsSet)
            {
                context.OnCommitted(async () =>
                                          {

                                              var client = GetClientFromTransactionContext(context);
                                              var messageSendRequests = outputQueue.GetMessages();
                                              var tasks = messageSendRequests.Select(r => client.SendMessageBatchAsync(new SendMessageBatchRequest(r.DestinationAddressUrl, r.Messages.ToList())));

                                              var response = await Task.WhenAll(tasks);
                                              if (response.Any(r => r.Failed.Any()))
                                              {
                                                  GenerateErrorsAndThrow(response);
                                              }

                                          });
                context.OnAborted(outputQueue.Clear);

                context.Items[OutgoingQueueContextActionIsSetKey] = true;
            }

            var sendMessageRequest = new SendMessageBatchRequestEntry()
                                     {

                                         MessageAttributes = CreateAttributesFromHeaders(message.Headers),
                                         MessageBody = GetBody(message.Body),
                                         Id = message.Headers.GetValueOrNull(Headers.MessageId) ?? Guid.NewGuid().ToString(),
                                     };

            outputQueue.AddMessage(GetDestinationQueueUrlByName(destinationAddress, context), sendMessageRequest);

            return _emptyTask;
        }

        private readonly Task _emptyTask = Task.FromResult(0);

        public async Task<TransportMessage> Receive(ITransactionContext context)
        {
            if (context == null) throw new ArgumentNullException("context");
            var client = GetClientFromTransactionContext(context);

            var response = await client.ReceiveMessageAsync(new ReceiveMessageRequest(_queueUrl)
                                                            {
                                                                MaxNumberOfMessages = 1,
                                                                WaitTimeSeconds = 1,
                                                                AttributeNames = new List<string>(new[] { "All" }),
                                                                MessageAttributeNames = new List<string>(new[] { "All" })
                                                            });



            if (response.Messages.Any())
            {
                var message = response.Messages.First();

                var renewalTask = CreateRenewalTaskForMessage(message, client);


                context.OnCompleted(async () =>
                {
                    renewalTask.Dispose();
                    var result = await client.DeleteMessageBatchAsync(new DeleteMessageBatchRequest(_queueUrl,
                        response.Messages
                            .Select(m => new DeleteMessageBatchRequestEntry(m.MessageId, m.ReceiptHandle))
                            .ToList()));

                    if (result.Failed.Any())
                    {
                        GenerateErrorsAndLog(result);
                    }
                });

                context.OnAborted(() =>
                {
                    renewalTask.Dispose();
                    var result = client.ChangeMessageVisibilityBatch(new ChangeMessageVisibilityBatchRequest(_queueUrl,
                        response.Messages
                        .Select(m => new ChangeMessageVisibilityBatchRequestEntry(m.MessageId, m.ReceiptHandle)
                        {
                            VisibilityTimeout = 0
                        }).ToList()));
                    if (result.Failed.Any())
                    {
                        GenerateErrorsAndLog(result);
                    }
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

            return null;

        }



        private AsyncTask CreateRenewalTaskForMessage(Message message, AmazonSQSClient client)
        {
            var renewalTask = new AsyncTask(string.Format("RenewPeekLock-{0}", message.MessageId),
                async () =>
                {
                    _log.Info("Renewing peek lock for message with ID {0}", message.MessageId);

                    await client.ChangeMessageVisibilityAsync(new ChangeMessageVisibilityRequest(_queueUrl, message.ReceiptHandle, (int)_peekLockDuration.TotalSeconds));
                },
                prettyInsignificant: true)
                              {
                                  Interval = _peekLockRenewalInterval
                              };
            return renewalTask;
        }

        private bool MessageIsExpired(Message message)
        {
            MessageAttributeValue value;
            TimeSpan timeToBeReceived;
            if (message.MessageAttributes.TryGetValue(Headers.TimeToBeReceived, out value))
            {
                timeToBeReceived = TimeSpan.Parse(value.StringValue);

                if (MessageIsExpiredUsingRebusSentTime(message, timeToBeReceived)) return true;
                if (MessageIsExpiredUsingNativeSqsSentTimestamp(message, timeToBeReceived)) return true;
            }

            return false;
        }

        private static bool MessageIsExpiredUsingRebusSentTime(Message message, TimeSpan timeToBeReceived)
        {
            MessageAttributeValue rebusUtcTimeSentAttributeValue;
            if (message.MessageAttributes.TryGetValue(Headers.SentTime, out rebusUtcTimeSentAttributeValue))
            {
                var rebusUtcTimeSent = DateTimeOffset.ParseExact(rebusUtcTimeSentAttributeValue.StringValue, "O", null);

                if (Time.RebusTime.Now.UtcDateTime - rebusUtcTimeSent > timeToBeReceived)
                {
                    return true;
                }

            }

            return false;

        }

        private static bool MessageIsExpiredUsingNativeSqsSentTimestamp(Message message, TimeSpan timeToBeReceived)
        {
            string sentTimeStampString;
            if (message.Attributes.TryGetValue("SentTimestamp", out sentTimeStampString))
            {
                var sentTime = GetTimeFromUnixTimestamp(sentTimeStampString);
                if (Time.RebusTime.Now.UtcDateTime - sentTime > timeToBeReceived)
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
            var headers = message.MessageAttributes.ToDictionary((kv) => kv.Key, (kv) => kv.Value.StringValue);

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
                                        value => new MessageAttributeValue() { DataType = "String", StringValue = value.Value });
        }
        private readonly ConcurrentDictionary<string, string> _queueUrls = new ConcurrentDictionary<string, string>();
        private string GetDestinationQueueUrlByName(string address, ITransactionContext transactionContext)
        {

            var url = _queueUrls.GetOrAdd("DestinationAddress" + address.ToLowerInvariant(), (key) =>
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
                        else
                        {
                            _log.Warn("Could not get queue url from service by name: {0}", address);
                            throw new ApplicationException(String.Format("could not find Url for address: {0} - got errorcode: {1}", address,
                                urlResponse.HttpStatusCode));
                        }

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

        public string Address
        {
            get { return _inputQueueAddress; }
        }

        private static void GenerateErrorsAndThrow(SendMessageBatchResponse[] response)
        {
            var failed = response.SelectMany(r => r.Failed);

            var failedMessages = String.Join("\n", failed.Select(f => String.Format("Code:{0}, Id:{1}, Error:{2}", f.Code, f.Id, f.Message)));
            var errorMessage = "There were 1 or more errors when sending messages on commit." + failedMessages;
            var successMessages = response.SelectMany(r => r.Successful).ToList();
            if (successMessages.Any())
                errorMessage += "\n These message went through the loophole:\n" + String.Join("\n", successMessages.Select(s => "   Id: " + s.Id + " MessageId:" + s.MessageId));

            throw new ApplicationException(errorMessage);
        }

        private void GenerateErrorsAndLog(DeleteMessageBatchResponse result)
        {

            var failedMessages = String.Join("\n", result.Failed.Select(f => String.Format("Code:{0}, Id:{1}, Error:{2}", f.Code, f.Id, f.Message)));
            var errorMessage = "There were 1 or more errors when sending messages on commit." + failedMessages;

            if (result.Successful.Any())
                errorMessage += "\n These message went through the loophole:\n" + String.Join(", ", result.Successful.Select(s => s.Id));

            _log.Warn("Not all completed messages is removed from the queue: {0} \n{1} failed.\n{2}", _inputQueueAddress, result.Failed.Count, errorMessage);
        }

        private void GenerateErrorsAndLog(ChangeMessageVisibilityBatchResponse result)
        {

            var failedMessages = String.Join("\n", result.Failed.Select(f => String.Format("Code:{0}, Id:{1}, Error:{2}", f.Code, f.Id, f.Message)));
            var errorMessage = "There were 1 or more errors when sending messages on commit." + failedMessages;

            if (result.Successful.Any())
                errorMessage += "\n These message went through the loophole:\n" + String.Join(", ", result.Successful.Select(s => s.Id));

            _log.Warn("Not all messages is set back to visible in the queue: {0} \n{1} failed.These will appear later when the global visibility time runs out. Details:\n{2}", _inputQueueAddress, result.Failed.Count, errorMessage);

        }

        public void DeleteQueue()
        {
            using (var client = new AmazonSQSClient(_accessKeyId, _secretAccessKey, _regionEndpoint))
            {
                client.DeleteQueue(_queueUrl);
            }
        }
    }
}
