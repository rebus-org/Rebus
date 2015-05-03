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




            context.OnCommitted(async () =>
                                {

                                    var client = GetClientFromTransactionContext(context);
                                    var messageSendRequests = _outputQueue.ToArray();

                                    var tasks = messageSendRequests.Select(r =>
                                            client.SendMessageBatchAsync(new SendMessageBatchRequest(r.Key, new List<SendMessageBatchRequestEntry>(r.Value)))
                                        );


                                    var response = await Task.WhenAll(tasks);

                                    if (response.Any(r => r.Failed.Any()))
                                    {
                                        GenerateErrorsAndThrow(response);
                                    }



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



            if (response.Messages.Any())
            {

                context.OnCommitted(async () =>
                {

                    var result = await client.DeleteMessageBatchAsync(new DeleteMessageBatchRequest(GetQueueUrl(_inputQueueAddress), response.Messages
                            .Select(m => new DeleteMessageBatchRequestEntry(m.MessageId, m.ReceiptHandle))
                            .ToList()));

                    if (result.Failed.Any())
                    {
                        GenerateErrorsAndLog(result);
                    }
                });

                context.OnAborted(() =>
                {

                    var result = client.ChangeMessageVisibilityBatch(new ChangeMessageVisibilityBatchRequest(GetQueueUrl(_inputQueueAddress), response.Messages
                        .Select(m => new ChangeMessageVisibilityBatchRequestEntry(m.MessageId, m.ReceiptHandle)
                        {
                            VisibilityTimeout = 0
                        })
                        .ToList()));
                    if (result.Failed.Any())
                    {
                        GenerateErrorsAndLog(result);
                    }
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

        private string GetQueueUrl(string address)
        {
            return _queueServiceUrl + address;
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

            _log.Warn("Not all completed messages is removed from the queue: {queue} \n{noOfFailedMessages} failed.\n {messageLog}", _inputQueueAddress, result.Failed.Count, errorMessage);
        }



        private void GenerateErrorsAndLog(ChangeMessageVisibilityBatchResponse result)
        {

            var failedMessages = String.Join("\n", result.Failed.Select(f => String.Format("Code:{0}, Id:{1}, Error:{2}", f.Code, f.Id, f.Message)));
            var errorMessage = "There were 1 or more errors when sending messages on commit." + failedMessages;

            if (result.Successful.Any())
                errorMessage += "\n These message went through the loophole:\n" + String.Join(", ", result.Successful.Select(s => s.Id));

            _log.Warn("Not all messages is set back to visible in the queue: {queue} \n{noOfFailedMessages} failed.These will appear later when the global visibility time runs out. Details:\n {messageLog}", _inputQueueAddress, result.Failed.Count, errorMessage);
        
        }
    }
}