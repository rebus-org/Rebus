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
                    var tasks = batches.Select(b =>
                                               {
                                                   var client = GetOrCreateClientFromContext(context, b.DestinationAddress);

                                                   return client.ExecuteBatchAsync(b.Operation);


                                               });


                    try
                    {
                        var response = await Task.WhenAll(tasks);
                        if (response.Any(r => r.Any(re => re.HttpStatusCode != 201)))
                        {
                            //TODO: Do errorhandling
                            throw new ApplicationException("something went wrong");
                            //GenerateErrorsAndThrow(response);
                        }
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

        private TransportMessageEntity GetEntityFromMessage(string destinationAddress, TransportMessage message)
        {
            return new TransportMessageEntity(destinationAddress, Time.RebusTime.Now.Ticks.ToString(), message.Headers, message.Body);
        }

        public async Task<TransportMessage> Receive(ITransactionContext context)
        {
            if (context == null) throw new ArgumentNullException("context");
            var client = GetOrCreateClientFromContext(context, _inputQueueAddress);

            var query = new TableQuery<TransportMessageEntity>()
                .Where(TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, _inputQueueAddress))
                .Take(5);

            var result = client.ExecuteQuery(query);

            foreach (var transportMessageEntity in result)
            {
                if (transportMessageEntity.LeaseTimeout < Time.RebusTime.Now)
                {

                    transportMessageEntity.LeaseTimeout = GetNewLeaseTimeOut();

                    var replaceOperation = TableOperation.Replace(transportMessageEntity);

                    var replaceResult = await client.ExecuteAsync(replaceOperation);
                    if (replaceResult.HttpStatusCode == 204)
                    {
                        TransportMessageEntity messageEntity = transportMessageEntity;
                        context.OnAborted(() =>
                                          {

                                              messageEntity.ResetLease();
                                              var resetLeaseQuery = TableOperation.Replace(messageEntity);
                                              client.ExecuteAsync(resetLeaseQuery);

                                          });

                        context.OnCommitted(() =>
                                            {
                                                var deleteQuery = TableOperation.Delete(messageEntity);
                                                return client.ExecuteAsync(deleteQuery);
                                            });

                        return GetMessageFromEntity(transportMessageEntity);
                    }

                }
            }


            return null;
        }

        private TransportMessage GetMessageFromEntity(TransportMessageEntity transportMessageEntity)
        {
            return new TransportMessage(transportMessageEntity.GetHeaders(), transportMessageEntity.Body);
        }

        private DateTime GetNewLeaseTimeOut()
        {
            return Time.RebusTime.Now.Add(TimeSpan.FromMinutes(5)).LocalDateTime;
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