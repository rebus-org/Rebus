using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.RetryPolicies;
using Microsoft.WindowsAzure.Storage.Table;
using Rebus.Bus;
using Rebus.Extensions;
using Rebus.Logging;
using Rebus.Messages;
using Rebus.Threading;
using Rebus.Transport;

namespace Rebus.AzureTableStorage
{
    public class AzureTableStorageTransport : ITransport, IInitializable
    {
        private readonly string _inputQueueAddress;

        static ILog _log;
        static AzureTableStorageTransport()
        {
            RebusLoggerFactory.Changed += f => _log = f.GetCurrentClassLogger();
        }

        private readonly CloudStorageAccount _cloudStorageAccount;
        private TableRequestOptions _tableRequestOptions = new TableRequestOptions() { PayloadFormat = TablePayloadFormat.Json, RetryPolicy = new LinearRetry(TimeSpan.FromMilliseconds(100), 5) };
        private CloudTableClient _cloudTableClient;
        private const string OutgoingQueueContextActionIsSetKey = "AzureTableStorageOutgoingQueueContextActionIsSetKey";
        private const string OutgoingQueueContextKey = "AzureTableStorageOutgoingQueueContextKey";
        public const string ClientContextKey = "AzureTableStorageClient";
        private TimeSpan _peekLockDuration = TimeSpan.FromMinutes(5);
        private TimeSpan _peekLockRenewalInterval = TimeSpan.FromMinutes(4);
        public AzureTableStorageTransport(string connectionString, string inputQueueAddress)
        {

            if (connectionString == null) throw new ArgumentNullException("connectionString");
            if (inputQueueAddress == null) throw new ArgumentNullException("inputQueueAddress");
            _inputQueueAddress = inputQueueAddress;
            _cloudStorageAccount = CloudStorageAccount.Parse(connectionString);
            _cloudTableClient = _cloudStorageAccount.CreateCloudTableClient();


        }

        public void CreateQueue(string address)
        {

            var aQueue = _cloudTableClient.GetTableReference(GetTableNameFromAddress(address));

            aQueue.CreateIfNotExists(_tableRequestOptions);


        }

        private static string GetTableNameFromAddress(string address)
        {
            return String.Format("Rebus{0}", address);
        }
        private readonly Task _emptyTask = Task.FromResult(0);
        public Task Send(string destinationAddress, TransportMessage message, ITransactionContext context)
        {


            var outputQueue = context.Items.GetOrAdd(OutgoingQueueContextKey, () => new InMemOutputQueue());
            var contextActionsSet = context.Items.GetOrAdd(OutgoingQueueContextActionIsSetKey, () => false);

            if (!contextActionsSet)
            {
                context.OnCommitted(async () =>
                {


                    var batches = outputQueue.GetBatchOperations();
                    // _log.Info("getting ready to send " + batches.Count() + " batches");
                    var tasks = batches.Select(b =>
                                               {
                                                   var client = GetOrCreateClientFromContext(context, b.DestinationAddress);

                                                   return client.ExecuteBatchAsync(b.Operation);


                                               });


                    try
                    {
                        await Task.WhenAll(tasks);
                        //  _log.Info("Batches send: " + tasks.Count());
                    }
                    catch (StorageException storageException)
                    {
                        Console.WriteLine(storageException.RequestInformation.ExtendedErrorInformation.ErrorMessage);
                        // _log.Error(storageException, "An error occurred on sending messages. Errorcode: {0}  \nhttpmessage: {1} \nexeptionmessage:{2} ", storageException.RequestInformation.HttpStatusCode, storageException.RequestInformation.HttpStatusMessage, storageException.Message);
                        throw;
                    }

                });
                context.OnAborted(outputQueue.Clear);

                context.Items[OutgoingQueueContextActionIsSetKey] = true;
            }


            var messageEntity = GetEntityFromMessage(destinationAddress, message);
            outputQueue.AddMessage(destinationAddress, messageEntity);

            return _emptyTask;


        }



        public async Task<TransportMessage> Receive(ITransactionContext context)
        {
            if (context == null) throw new ArgumentNullException("context");
            var client = GetOrCreateClientFromContext(context, _inputQueueAddress);

            var query = new TableQuery<TransportMessageEntity>()
                .Where(TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, _inputQueueAddress));


            //TableContinuationToken continuationToken = null;

            //do
            //{
            //    // Retrieve a segment (up to 1000 entities)
            //    TableQuerySegment<TransportMessageEntity> tableQueryResult =
            //        await client.ExecuteQuerySegmentedAsync(query, continuationToken);

            //    // Assign the new continuation token to tell the service where to 
            //    // continue on the next iteration (or null if it has reached the end)
            //    continuationToken = tableQueryResult.ContinuationToken;

            //    // Print the number of rows retrieved
            //    Console.WriteLine("Rows retrieved {0}", tableQueryResult.Results.Count);

            //    // Loop until a null continuation token is received indicating the end of the table
            //} while (continuationToken != null);


            var result = client.ExecuteQuery(query);//, continueToken);//.Where(e => e.LeaseTimeout < Time.RebusTime.Now);

            foreach (var transportMessageEntity in result)
            {


                if (transportMessageEntity.LeaseTimeout < Time.RebusTime.Now)
                {
                    if (MessageIsExpired(transportMessageEntity))
                    {
                        _log.Warn("message expired");
                        var deleteQuery = TableOperation.Delete(transportMessageEntity);
                        await client.ExecuteAsync(deleteQuery);
                        continue;
                    }

                    transportMessageEntity.LeaseTimeout = GetNewLeaseTimeOut();

                    var replaceOperation = TableOperation.Replace(transportMessageEntity);

                    try
                    {

                        var replaceResult = await client.ExecuteAsync(replaceOperation);
                        if (replaceResult.HttpStatusCode == 204)
                        {

                            var renewalTask = CreateRenewalTaskForMessage(transportMessageEntity, client);
                            TransportMessageEntity messageEntity = transportMessageEntity;
                            context.OnAborted(() =>
                                              {
                                                  renewalTask.Dispose();
                                                  messageEntity.ResetLease();
                                                  var resetLeaseQuery = TableOperation.Replace(messageEntity);
                                                  client.ExecuteAsync(resetLeaseQuery);

                                              });

                            context.OnCommitted(async () =>
                                                {

                                                    renewalTask.Dispose();
                                                    var deleteQuery = TableOperation.Delete(messageEntity);
                                                    await client.ExecuteAsync(deleteQuery);
                                                    //_log.Info("message received");
                                                });
                            renewalTask.Start();
                            return GetMessageFromEntity(transportMessageEntity);
                        }
                    }
                    catch (StorageException exception)
                    {
                        //just log.. trying next operation... Might check for right concurrency exception

                    }

                }
            }


            return null;
        }

        private AsyncTask CreateRenewalTaskForMessage(TransportMessageEntity transportMessageEntity, CloudTable client)
        {
            var renewalTask = new AsyncTask(string.Format("RenewPeekLock-{0}", transportMessageEntity.RowKey),
                async () =>
                {
                    _log.Info("Renewing peek lock for message with ID {0}", transportMessageEntity.RowKey);

                    transportMessageEntity.LeaseTimeout = GetNewLeaseTimeOut();
                    var replaceQuery = TableOperation.Replace(transportMessageEntity);
                    await client.ExecuteAsync(replaceQuery);

                },
                prettyInsignificant: true)
            {
                Interval = _peekLockRenewalInterval
            };
            return renewalTask;
        }

        private TransportMessage GetMessageFromEntity(TransportMessageEntity transportMessageEntity)
        {
            return new TransportMessage(transportMessageEntity.GetHeaders(), transportMessageEntity.Body);
        }
        private TransportMessageEntity GetEntityFromMessage(string destinationAddress, TransportMessage message)
        {
            return new TransportMessageEntity(destinationAddress, Guid.NewGuid().ToString() + Time.RebusTime.Now.Ticks.ToString(), message.Headers, message.Body);
        }
        private DateTime GetNewLeaseTimeOut()
        {
            return Time.RebusTime.Now.Add(_peekLockDuration).LocalDateTime;
        }



        private bool MessageIsExpired(TransportMessageEntity message)
        {

            string headerValue;
            if (message.GetHeaders().TryGetValue(Headers.TimeToBeReceived, out headerValue))
            {
                TimeSpan timeToBeReceived = TimeSpan.Parse(headerValue);

                // if (MessageIsExpiredUsingRebusSentTime(message, timeToBeReceived)) return true;
                if (MessageIsExpiredUsingNativMessageSentTime(message, timeToBeReceived)) return true;

            }

            return false;
        }

        private bool MessageIsExpiredUsingNativMessageSentTime(TransportMessageEntity message, TimeSpan timeToBeReceived)
        {
            if (Time.RebusTime.Now.UtcDateTime - message.SentTime > timeToBeReceived)
            {
                return true;
            }

            return false;
        }

        private static bool MessageIsExpiredUsingRebusSentTime(TransportMessageEntity message, TimeSpan timeToBeReceived)
        {
            string rebusUtcTimeSentAttributeValue = null;
            if (message.GetHeaders().TryGetValue(Headers.SentTime, out rebusUtcTimeSentAttributeValue))
            {
                var rebusUtcTimeSent = DateTimeOffset.ParseExact(rebusUtcTimeSentAttributeValue, "O", null);

                if (Time.RebusTime.Now.UtcDateTime - rebusUtcTimeSent > timeToBeReceived)
                {
                    return true;
                }

            }

            return false;

        }


        private CloudTable GetOrCreateClientFromContext(ITransactionContext context, string queueAddress)
        {

            return context.Items.GetOrAdd(ClientContextKey + queueAddress.ToLowerInvariant(), () => _cloudTableClient.GetTableReference(GetTableNameFromAddress(queueAddress)));
        }



        public string Address
        {
            get
            {
                return
                    _inputQueueAddress;
            }
        }

        public void PurgeInputQueue()
        {
            try
            {
                var table = _cloudTableClient.GetTableReference(GetTableNameFromAddress(_inputQueueAddress));
                var query = new TableQuery<TransportMessageEntity>()
                    .Where(TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, _inputQueueAddress));

                var allEntities = table.ExecuteQuery(query);

                var batchOperation = new TableBatchOperation();
                bool any = false;
                foreach (var entity in allEntities)
                {
                    any = true;
                    batchOperation.Delete(entity);
                }
                if (any)
                    table.ExecuteBatch(batchOperation);
            }
            catch (StorageException exception)
            {
                Console.WriteLine("{0} {1} {2}", exception.Message, exception.RequestInformation.HttpStatusCode, exception.RequestInformation.HttpStatusMessage);

                if (exception.RequestInformation.HttpStatusCode != 404)
                    throw;
            }

        }

        public void Initialize()
        {
            CreateQueue(_inputQueueAddress);
        }


    }
}