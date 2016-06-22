using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage.RetryPolicies;
using Microsoft.WindowsAzure.Storage.Table;
using Newtonsoft.Json;
using Rebus.Exceptions;
using Rebus.Logging;
using Rebus.Sagas;

namespace Rebus.AzureStorage.Sagas
{
    public class AzureStorageSagaStorage : ISagaStorage
    {
        static readonly JsonSerializerSettings Settings = new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.All };
        const string IdPropertyName = nameof(ISagaData.Id);
        private const string RevisionKey = "revision";
        private readonly CloudStorageAccount _cloudStorageAccount;
        private readonly string _tableName;
        private readonly string _containerName;

        public void EnsureCreated()
        {
            var cloudTableClient = _cloudStorageAccount.CreateCloudTableClient();
            var cloudBlobClient = _cloudStorageAccount.CreateCloudBlobClient();
            var cont = cloudBlobClient.GetContainerReference(_containerName);
            var t = cloudTableClient.GetTableReference(_tableName);
            t.CreateIfNotExists();
            cont.CreateIfNotExists();
        }

        public CloudTable GetTable()
        {

            var client = _cloudStorageAccount.CreateCloudTableClient();

            return client.GetTableReference(_tableName);

        }

        public AzureStorageSagaStorage(CloudStorageAccount cloudStorageAccount,
            string tableName = "RebusSagaIndex", 
            string containerName = "RebusSagaStorage")
        {
            _cloudStorageAccount = cloudStorageAccount;
            _tableName = tableName;
            _containerName = containerName.ToLowerInvariant();
            EnsureCreated();
        }
        
        public async Task<ISagaData> Find(Type sagaDataType, string propertyName, object propertyValue)
        {
            try
            {
                if (propertyName.Equals(IdPropertyName, StringComparison.InvariantCultureIgnoreCase))
                {
                    string sagaId = propertyValue is string
                        ? (string) propertyValue
                        : ((Guid) propertyValue).ToString("N");
                    return await ReadSaga(sagaId, sagaDataType);
                }
                var q = CreateFindQuery(sagaDataType, propertyName, propertyValue);
                var table = GetTable();
                var sagas =
                    await
                        table.ExecuteQueryAsync<DynamicTableEntity>(q,
                            new TableRequestOptions {RetryPolicy = new ExponentialRetry()}, new OperationContext());
                var id = sagas.Select(x => x.Properties["SagaId"].StringValue).FirstOrDefault();
                if (String.IsNullOrEmpty(id))
                {
                    return null;
                }
                return await ReadSaga(id, sagaDataType);
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        private async Task<ISagaData> ReadSaga(string sagaId, Type sagaDataType)
        {
            var dataRef = $"{sagaId}/data.json";
            
            var dataBlob = CloudBlobContainer.GetBlockBlobReference(dataRef);
            if (!await dataBlob.ExistsAsync()) return null;
            var data = await dataBlob.DownloadTextAsync(Encoding.Unicode, new AccessCondition(),
                new BlobRequestOptions {RetryPolicy = new ExponentialRetry()}, new OperationContext());
            try
            {
                return (ISagaData) JsonConvert.DeserializeObject(data, Settings);
            }
            catch (Exception exception)
            {
                throw new ApplicationException(
                    $"An error occurred while attempting to deserialize '{data}' into a {sagaDataType}", exception);
            }
        }

        private CloudBlobContainer CloudBlobContainer
        {
            get
            {
                var client = _cloudStorageAccount.CreateCloudBlobClient();
                var container = client.GetContainerReference(_containerName);
                return container;
            }
        }


        public async Task Insert(ISagaData sagaData, IEnumerable<ISagaCorrelationProperty> correlationProperties)
        {
            //TODO: Implement concurrency
            if (sagaData.Id == Guid.Empty)
            {
                throw new InvalidOperationException($"Saga data {sagaData.GetType()} has an uninitialized Id property!");
            }

            if (sagaData.Revision != 0)
            {
                throw new InvalidOperationException($"Attempted to insert saga data with ID {sagaData.Id} and revision {sagaData.Revision}, but revision must be 0 on first insert!");
            }


            var dataBlob = GetSagaDataBlob(sagaData.Id);
            //if (await dataBlob.ExistsAsync() && dataBlob.Metadata["revision"] != revisionToUpdate.ToString())
            //{
            //    throw new ConcurrencyException("Update of saga with ID {0} did not succeed because someone else beat us to it", sagaData.Id);
            //}

            dataBlob.Properties.ContentType = "application/json";
            dataBlob.Metadata[RevisionKey] = sagaData.Revision.ToString();
            
            await
                dataBlob.UploadTextAsync(JsonConvert.SerializeObject(sagaData, Settings), Encoding.Unicode,
                    AccessCondition.GenerateEmptyCondition(),
                    new BlobRequestOptions { RetryPolicy = new ExponentialRetry() }, new OperationContext());
            await dataBlob.SetPropertiesAsync(AccessCondition.GenerateEmptyCondition(),
                new BlobRequestOptions { RetryPolicy = new ExponentialRetry() }, new OperationContext());
            await dataBlob.SetMetadataAsync(AccessCondition.GenerateEmptyCondition(),
                new BlobRequestOptions { RetryPolicy = new ExponentialRetry() }, new OperationContext());


            await InsertSagaCorrelationProperties(sagaData, correlationProperties);
        }

        private async Task InsertSagaCorrelationProperties(ISagaData sagaData, IEnumerable<ISagaCorrelationProperty> correlationProperties)
        {
            try
            {
                var t = GetTable();
                var operationContext = new OperationContext();
                var tableRequestOptions = new TableRequestOptions { RetryPolicy = new ExponentialRetry() };
                var indices = CreateIndices(sagaData, correlationProperties);
                foreach (var i in indices)
                {
                    var res = await t.ExecuteAsync(TableOperation.InsertOrReplace(i), tableRequestOptions, operationContext);
                }
            }
            catch (Exception exception)
            {
                throw new RebusApplicationException(exception, $"Could not subscribe create the index for Saga {sagaData.Id}");
            }
        }

        public async Task Delete(ISagaData sagaData)
        {
            var options = new BlobRequestOptions { RetryPolicy = new ExponentialRetry() };
            var context = new OperationContext();

            var dataBlob = GetSagaDataBlob(sagaData.Id);
            if (!await dataBlob.ExistsAsync())
            {
                return;
            }
            string leaseId = await TakeOutSagaLease(dataBlob);

            await dataBlob.FetchAttributesAsync();
            if (dataBlob.Metadata[RevisionKey] != sagaData.Revision.ToString())
            {
                throw new ConcurrencyException("Update of saga with ID {0} did not succeed because someone else beat us to it", sagaData.Id);
            }
            sagaData.Revision++;
            var cloudTable = GetTable();
            await ClearSagaIndex(sagaData, cloudTable);

            var condition = AccessCondition.GenerateLeaseCondition(leaseId);
            await dataBlob.DeleteAsync(DeleteSnapshotsOption.None, condition, options, context);
        }

        public async Task Update(ISagaData sagaData, IEnumerable<ISagaCorrelationProperty> correlationProperties)
        {
            var revisionToUpdate = sagaData.Revision;




            var options = new BlobRequestOptions { RetryPolicy = new ExponentialRetry() };
            var context = new OperationContext();

            var dataBlob = GetSagaDataBlob(sagaData.Id);
            string leaseId = null;
            try
            {
                leaseId = await TakeOutSagaLease(dataBlob);
                await dataBlob.FetchAttributesAsync();
                if (dataBlob.Metadata[RevisionKey] != revisionToUpdate.ToString())
                {
                    throw new ConcurrencyException("Update of saga with ID {0} did not succeed because someone else beat us to it", sagaData.Id);
                }
                sagaData.Revision++;
                var cloudTable = GetTable();
                await ClearSagaIndex(sagaData, cloudTable);
                dataBlob.Properties.ContentType = "application/json";
                dataBlob.Metadata[RevisionKey] = sagaData.Revision.ToString();
                var condition = AccessCondition.GenerateLeaseCondition(leaseId);

                var data = JsonConvert.SerializeObject(sagaData, Settings);
                await dataBlob.UploadTextAsync(data, Encoding.Unicode, condition, options, context);
                await dataBlob.SetPropertiesAsync(condition, options, context);
                await dataBlob.SetMetadataAsync(condition, options, context);


                await InsertSagaCorrelationProperties(sagaData, correlationProperties);
            }
            finally
            {
                if (!String.IsNullOrEmpty(leaseId))
                {
                    var condition = AccessCondition.GenerateLeaseCondition(leaseId);
                    await dataBlob.ReleaseLeaseAsync(condition, options, context);
                }
            }
        }


        private async Task<string> TakeOutSagaLease(CloudBlockBlob dataBlob)
        {
            return await dataBlob.AcquireLeaseAsync(null);
        }

        private CloudBlockBlob GetSagaDataBlob(Guid sagaDataId)
        {
            var dataRef = $"{sagaDataId:N}/data.json";
            var client = _cloudStorageAccount.CreateCloudBlobClient();
            var container = client.GetContainerReference(_containerName);
            var dataBlob = container.GetBlockBlobReference(dataRef);
            return dataBlob;
        }

        private async Task ClearSagaIndex(ISagaData sagaData, CloudTable table)
        {
            var partitionKey = sagaData.GetType().Name;
            var op = TableOperation.Retrieve<DynamicTableEntity>(partitionKey, $"{sagaData.Id:N}_{sagaData.Revision:0000000000}");

            var operationContext = new OperationContext();
            var tableRequestOptions = new TableRequestOptions { RetryPolicy = new ExponentialRetry() };

            var res = await table.ExecuteAsync(op, tableRequestOptions, operationContext);
            if (res != null && res.Result != null)
            {
                var index = (DynamicTableEntity) res.Result;
                var entries = GetIndicies(index, partitionKey);
                foreach (var e in entries)
                {
                    await table.ExecuteAsync(TableOperation.Delete(e), tableRequestOptions, operationContext);
                }
                await table.ExecuteAsync(TableOperation.Delete(index), tableRequestOptions, operationContext);
            }
        }





        internal static List<DynamicTableEntity> CreateIndices(ISagaData sagaData, IEnumerable<ISagaCorrelationProperty> correlationProperties)
        {
            var sagaTypeName = sagaData.GetType().Name;
            var sagaId = sagaData.Id.ToString("N");
            var propertiesToIndex = GetPropertiesToIndex(sagaData, correlationProperties);
            var indexEntries = Enumerable.Select<KeyValuePair<string, string>, DynamicTableEntity>(propertiesToIndex, p => new DynamicTableEntity(sagaTypeName, $"{p.Key}_{(p.Value ?? "").ToString()}", "*",
                new Dictionary<string, EntityProperty>
                {
                    {"SagaId", new EntityProperty(sagaId)},
                    {"Revision", new EntityProperty(sagaData.Revision)}
                }
                ));
            var reverseIndex = new DynamicTableEntity(sagaTypeName, $"{sagaId}_{sagaData.Revision:0000000000}", "*", Enumerable.ToDictionary<KeyValuePair<string, string>, string, EntityProperty>(propertiesToIndex, x => x.Key, x => new EntityProperty(x.Value)));
            return new[] { reverseIndex }.Concat(indexEntries).ToList();
        }

        public static List<DynamicTableEntity> GetIndicies(DynamicTableEntity te, string sagaTypeName)
        {
            return te.Properties.Select(p => new DynamicTableEntity(sagaTypeName, $"{p.Key}_{(p.Value.StringValue ?? "")}")).ToList();
        }

        public static object GetPropertyValue(object obj, string path)
        {
            var dots = path.Split('.');

            foreach (var dot in dots)
            {
                var propertyInfo = obj.GetType().GetProperty(dot);
                if (propertyInfo == null) return null;
                obj = propertyInfo.GetValue(obj, new object[0]);
                if (obj == null) break;
            }

            return obj;
        }

        private static List<KeyValuePair<string, string>> GetPropertiesToIndex(ISagaData sagaData, IEnumerable<ISagaCorrelationProperty> correlationProperties)
        {
            return correlationProperties
                .Select(p => p.PropertyName)
                .Select(path =>
                {
                    var value = GetPropertyValue(sagaData, path);

                    return new KeyValuePair<string, string>(path, value != null ? value.ToString() : null);
                })
                .Where(kvp => kvp.Value != null)
                .ToList();
        }

        public static TableQuery<DynamicTableEntity> CreateFindQuery(Type sagaDataType, string propertyName, object propertyValue)
        {

            var prefixCondition = TableQuery.GenerateFilterCondition("RowKey",
                QueryComparisons.Equal,
                $"{propertyName}_{(propertyValue == null ? "" : propertyValue.ToString())}");

            var filterString = TableQuery.CombineFilters(
                TableQuery.GenerateFilterCondition("PartitionKey",
                    QueryComparisons.Equal,
                    sagaDataType.Name),
                TableOperators.And,
                prefixCondition
                );

            return new TableQuery<DynamicTableEntity>()
                .Where(filterString);


        }
    }
}
