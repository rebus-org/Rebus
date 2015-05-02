using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using Amazon;
using Amazon.IdentityManagement.Model;
using Amazon.SQS;
using Amazon.SQS.Model;
using Rebus.Extensions;
using Rebus.Logging;
using Rebus.Messages;
using Rebus.Transport;
using Message = Amazon.SQS.Model.Message;

namespace Rebus.AmazonSQS
{
    public class AmazonSqsTransport : ITransport
    {
        static ILog _log;
        static AmazonSqsTransport()
        {
            RebusLoggerFactory.Changed += f => _log = f.GetCurrentClassLogger();
        }

        private readonly string _inputQueueAddress;
        private readonly string _accessKeyId;
        private readonly string _secretAccessKey;
        private readonly string _queueServiceUrl;
        private readonly RegionEndpoint _regionEndpoint;
        private const string ClientContextKey = "SQS_Client";

        public AmazonSqsTransport(string inputQueueAddress, string accessKeyId, string secretAccessKey, string queueServiceUrl, RegionEndpoint regionEndpoint)
        {
            _inputQueueAddress = inputQueueAddress;
            _accessKeyId = accessKeyId;
            _secretAccessKey = secretAccessKey;
            _queueServiceUrl = queueServiceUrl;
            _regionEndpoint = regionEndpoint;
        }

        public void CreateQueue(string address)
        {
            // var queueUrl = GetQueueUrl(address);

            using (var client = new AmazonSQSClient(_accessKeyId, _secretAccessKey, new AmazonSQSConfig()
                                                                                    {
                                                                                        ServiceURL = _queueServiceUrl,
                                                                                        RegionEndpoint = _regionEndpoint
                                                                                    }))
            {

                var response = client.CreateQueue(address);

                if (response.HttpStatusCode != HttpStatusCode.OK)
                {
                    //TODO: unwrap metadata
                    _log.Warn("Did not create queue with {address} - there was an error: ErrorCode: {errorCode} and message: {message}", address, response.HttpStatusCode, response.ResponseMetadata.Metadata.FirstOrDefault());
                }


            }
        }

        public void Initialize()
        {

            CreateQueue(_inputQueueAddress);
        }
        public void Purge()
        {
            using (var client = new AmazonSQSClient(_accessKeyId, _secretAccessKey, RegionEndpoint.EUCentral1))
            {


                try
                {
                    var response = client.ReceiveMessage(new ReceiveMessageRequest(GetQueueUrl(_inputQueueAddress))
                                          {
                                              MaxNumberOfMessages = 10
                                          });

                    while (response.Messages.Any())
                    {
                        var deleteResponse = client.DeleteMessageBatch(GetQueueUrl(_inputQueueAddress), response.Messages
                        .Select(m => new DeleteMessageBatchRequestEntry(m.MessageId, m.ReceiptHandle))
                        .ToList());

                        if (!deleteResponse.Failed.Any())
                        {
                            response = client.ReceiveMessage(new ReceiveMessageRequest(GetQueueUrl(_inputQueueAddress))
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
        private ConcurrentDictionary<string, List<SendMessageBatchRequestEntry>> _outputQueue = new ConcurrentDictionary<string, List<SendMessageBatchRequestEntry>>();
        public async Task Send(string destinationAddress, TransportMessage message, ITransactionContext context)
        {




            context.OnCommitted(() =>
                                {

                                    var client = GetClientFromTransactionContext(context);
                                    var messageSendRequests = _outputQueue.ToArray();

                                    var tasks = messageSendRequests.Select(r =>
                                            client.SendMessageBatchAsync(new SendMessageBatchRequest(r.Key, new List<SendMessageBatchRequestEntry>(r.Value)))
                                        );


                                    return Task.WhenAll(tasks);

                                });
            context.OnAborted(() =>
                              {
                                  _outputQueue = new ConcurrentDictionary<string, List<SendMessageBatchRequestEntry>>();
                              });



            var sendMessageRequest = new SendMessageBatchRequestEntry()
                                     {

                                         MessageAttributes = CreateAttributesFromHeaders(message.Headers),
                                         MessageBody = GetBody(message.Body),
                                         Id = message.Headers.GetValueOrNull(Headers.MessageId) ?? Guid.NewGuid().ToString()


                                     };

            _outputQueue.AddOrUpdate(GetQueueUrl(destinationAddress),
                                    (key) => new List<SendMessageBatchRequestEntry>(new[] { sendMessageRequest }),
                                    (key, list) =>
                                    {
                                        list.Add(sendMessageRequest);
                                        return list;
                                    });




            //var response = await client.SendMessageAsync(sendMessageRequest);

            ////TODO: transactions
            //if (response.HttpStatusCode != HttpStatusCode.OK)
            //{
            //    //TODO: unwrap metadata
            //    _log.Error("There's an error in sending messages: ErrorCode: {errorCode} and message: {message}", response.HttpStatusCode, response.ResponseMetadata.Metadata.FirstOrDefault());
            //}

        }




        public async Task<TransportMessage> Receive(ITransactionContext context)
        {
            var client = GetClientFromTransactionContext(context);


            var response = await client.ReceiveMessageAsync(new ReceiveMessageRequest(GetQueueUrl(_inputQueueAddress))
                                                            {
                                                                MaxNumberOfMessages = 1,
                                                                WaitTimeSeconds = 1,
                                                                AttributeNames = new List<string>(new[] { "All" }),
                                                                MessageAttributeNames = new List<string>(new[] { "All" })
                                                            });





            //if (response.HttpStatusCode != HttpStatusCode.OK)
            //{
            //    _log.Error("There's an error in sending messages: ErrorCode: {errorCode} and message: {message}", response.HttpStatusCode, response.ResponseMetadata.Metadata.FirstOrDefault());

            //}
            //else
            //{

            if (response.Messages.Any())
            {

                context.OnCommitted(() =>
                {
                    return
                        client.DeleteMessageBatchAsync(new DeleteMessageBatchRequest(GetQueueUrl(_inputQueueAddress), response.Messages
                            .Select(m => new DeleteMessageBatchRequestEntry(m.MessageId, m.ReceiptHandle))
                            .ToList()));


                });

                context.OnAborted(() =>
                {

                    var result = client.ChangeMessageVisibilityBatch(new ChangeMessageVisibilityBatchRequest(GetQueueUrl(_inputQueueAddress), response.Messages
                        .Select(m => new ChangeMessageVisibilityBatchRequestEntry(m.MessageId, m.ReceiptHandle)
                        {
                            VisibilityTimeout = 0
                        })
                        .ToList()));

                });
                var transportMessage = GetTransportMessage(response.Messages.First());

                return transportMessage;
            }


            return null;

        }
        private AmazonSQSClient GetClientFromTransactionContext(ITransactionContext context)
        {
            return context.Items.GetOrAdd(ClientContextKey, () =>
            {
                var amazonSqsClient = new AmazonSQSClient(_accessKeyId, _secretAccessKey, new AmazonSQSConfig()
                {
                    ServiceURL = _queueServiceUrl,
                    RegionEndpoint = _regionEndpoint
                });
                context.OnDisposed(amazonSqsClient.Dispose);
                return amazonSqsClient;
            });
        }



        private TransportMessage GetTransportMessage(Message message)
        {
            //TODO: Attributes == headers?
            var headers = message.MessageAttributes.ToDictionary((kv)=>kv.Key,(kv)=> kv.Value.StringValue);

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

        private string GetQueueUrl(string address)
        {
            return _queueServiceUrl + address;
        }

        public string Address
        {
            get { return _inputQueueAddress; }
        }
    }
}