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
using Rebus.Bus;
using Rebus.Exceptions;
using Rebus.Logging;
using Rebus.Sagas;

namespace Rebus.AzureStorage.Sagas
{
    /// <summary>
    /// Implementation of <see cref="ISagaStorage"/> that uses
    /// </summary>
    public class AzureStorageSagaStorage : ISagaStorage, IInitializable
    {
        const string IdPropertyName = nameof(ISagaData.Id);
        const string RevisionKey = "revision";

        static readonly JsonSerializerSettings Settings = new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.All };
        static readonly Encoding TextEncoding = Encoding.UTF8;

        readonly CloudBlobContainer _containerReference;
        readonly CloudTable _tableReference;
        readonly ILog _log;

        /// <summary>
        /// Creates the saga storage
        /// </summary>
        public AzureStorageSagaStorage(CloudStorageAccount cloudStorageAccount,
            IRebusLoggerFactory loggerFactory,
            string tableName = "RebusSagaIndex",
            string containerName = "RebusSagaStorage")
        {
            if (cloudStorageAccount == null) throw new ArgumentNullException(nameof(cloudStorageAccount));
            if (loggerFactory == null) throw new ArgumentNullException(nameof(loggerFactory));
            if (tableName == null) throw new ArgumentNullException(nameof(tableName));
            if (containerName == null) throw new ArgumentNullException(nameof(containerName));

            _tableReference = cloudStorageAccount.CreateCloudTableClient().GetTableReference(tableName.ToLowerInvariant());
            _containerReference = cloudStorageAccount.CreateCloudBlobClient().GetContainerReference(containerName.ToLowerInvariant());
            _log = loggerFactory.GetCurrentClassLogger();
        }

        /// <summary>
        /// Initializes the storage by ensuring that the necessary container and table exist
        /// </summary>
        public void Initialize()
        {
            EnsureCreated();
        }

        void EnsureCreated()
        {
            try
            {
                if (_containerReference.CreateIfNotExists())
                {
                    _log.Info("Container '{0}' was automatically created", _containerReference.Name);
                }

                if (_tableReference.CreateIfNotExists())
                {
                    _log.Info("Table '{0}' was automatically created", _tableReference.Name);
                }
            }
            catch (Exception exception)
            {
                throw new RebusApplicationException(exception, $"Could not initialize Azure saga storage with container '{_containerReference.Name}' and table '{_tableReference.Name}'");
            }
        }

        /// <summary>
        /// Looks up a saga data instance of the given type that has the specified property name/value
        /// </summary>
        public async Task<ISagaData> Find(Type sagaDataType, string propertyName, object propertyValue)
        {
            if (propertyName.Equals(IdPropertyName, StringComparison.InvariantCultureIgnoreCase))
            {
                var sagaId = propertyValue is string
                    ? (string)propertyValue
                    : ((Guid)propertyValue).ToString("N");

                var sagaData = await ReadSaga(sagaId, sagaDataType);

                // in this case, we need to filter on the saga data type
                if (!sagaDataType.IsInstanceOfType(sagaData))
                {
                    return null;
                }

                return sagaData;
            }
            var query = CreateFindQuery(sagaDataType, propertyName, propertyValue);
            var sagas = await _tableReference.ExecuteQueryAsync(query, DefaultTableRequestOptions, DefaultOperationContext);
            var id = sagas.Select(x => x.Properties["SagaId"].StringValue).FirstOrDefault();

            if (string.IsNullOrEmpty(id))
            {
                return null;
            }

            return await ReadSaga(id, sagaDataType);
        }

        /// <summary>
        /// Inserts the saga data
        /// </summary>
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

            var jsonText = JsonConvert.SerializeObject(sagaData, Settings);

            await dataBlob.UploadTextAsync(jsonText, TextEncoding, DefaultAccessCondition, DefaultBlobRequestOptions, DefaultOperationContext);

            await dataBlob.SetPropertiesAsync(DefaultAccessCondition,
                new BlobRequestOptions { RetryPolicy = new ExponentialRetry() }, new OperationContext());
            await dataBlob.SetMetadataAsync(AccessCondition.GenerateEmptyCondition(),
                new BlobRequestOptions { RetryPolicy = new ExponentialRetry() }, new OperationContext());


            await InsertSagaCorrelationProperties(sagaData, correlationProperties);
        }

        /// <summary>
        /// Updates the saga data and increments its revision number
        /// </summary>
        public async Task Update(ISagaData sagaData, IEnumerable<ISagaCorrelationProperty> correlationProperties)
        {
            var revisionToUpdate = sagaData.Revision;

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
                await ClearSagaIndex(sagaData, _tableReference);
                dataBlob.Properties.ContentType = "application/json";
                dataBlob.Metadata[RevisionKey] = sagaData.Revision.ToString();
                var condition = AccessCondition.GenerateLeaseCondition(leaseId);
                var data = JsonConvert.SerializeObject(sagaData, Settings);

                await dataBlob.UploadTextAsync(data, TextEncoding, condition, DefaultBlobRequestOptions, DefaultOperationContext);
                await dataBlob.SetPropertiesAsync(condition, DefaultBlobRequestOptions, DefaultOperationContext);
                await dataBlob.SetMetadataAsync(condition, DefaultBlobRequestOptions, DefaultOperationContext);

                await InsertSagaCorrelationProperties(sagaData, correlationProperties);
            }
            finally
            {
                if (!string.IsNullOrEmpty(leaseId))
                {
                    var condition = AccessCondition.GenerateLeaseCondition(leaseId);
                    await dataBlob.ReleaseLeaseAsync(condition, DefaultBlobRequestOptions, DefaultOperationContext);
                }
            }
        }

        /// <summary>
        /// Deletes the saga data
        /// </summary>
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
            await ClearSagaIndex(sagaData, _tableReference);

            var condition = AccessCondition.GenerateLeaseCondition(leaseId);
            await dataBlob.DeleteAsync(DeleteSnapshotsOption.None, condition, options, context);
        }

        static OperationContext DefaultOperationContext => new OperationContext();

        static TableRequestOptions DefaultTableRequestOptions => new TableRequestOptions { RetryPolicy = new ExponentialRetry() };

        static BlobRequestOptions DefaultBlobRequestOptions => new BlobRequestOptions { RetryPolicy = new ExponentialRetry() };

        static AccessCondition DefaultAccessCondition => AccessCondition.GenerateEmptyCondition();

        async Task<ISagaData> ReadSaga(string sagaId, Type sagaDataType)
        {
            var dataRef = $"{sagaId}/data.json";

            var dataBlob = _containerReference.GetBlockBlobReference(dataRef);
            if (!await dataBlob.ExistsAsync()) return null;
            var data = await dataBlob.DownloadTextAsync(TextEncoding, DefaultAccessCondition, DefaultBlobRequestOptions, DefaultOperationContext);
            try
            {
                return (ISagaData)JsonConvert.DeserializeObject(data, Settings);
            }
            catch (Exception exception)
            {
                throw new ApplicationException(
                    $"An error occurred while attempting to deserialize '{data}' into a {sagaDataType}", exception);
            }
        }

        CloudBlockBlob GetSagaDataBlob(Guid sagaDataId)
        {
            var dataRef = $"{sagaDataId:N}/data.json";
            return _containerReference.GetBlockBlobReference(dataRef);
        }

        async Task InsertSagaCorrelationProperties(ISagaData sagaData, IEnumerable<ISagaCorrelationProperty> correlationProperties)
        {
            try
            {
                var operationContext = new OperationContext();
                var tableRequestOptions = new TableRequestOptions { RetryPolicy = new ExponentialRetry() };
                var indices = CreateIndices(sagaData, correlationProperties);
                foreach (var i in indices)
                {
                    var res = await _tableReference.ExecuteAsync(TableOperation.InsertOrReplace(i), tableRequestOptions, operationContext);
                }
            }
            catch (Exception exception)
            {
                throw new RebusApplicationException(exception, $"Could not subscribe create the index for Saga {sagaData.Id}");
            }
        }

        async Task<string> TakeOutSagaLease(CloudBlockBlob dataBlob)
        {
            return await dataBlob.AcquireLeaseAsync(null);
        }

        static async Task ClearSagaIndex(ISagaData sagaData, CloudTable table)
        {
            var partitionKey = sagaData.GetType().Name;
            var op = TableOperation.Retrieve<DynamicTableEntity>(partitionKey, $"{sagaData.Id:N}_{sagaData.Revision:0000000000}");

            var operationContext = new OperationContext();
            var tableRequestOptions = new TableRequestOptions { RetryPolicy = new ExponentialRetry() };

            var res = await table.ExecuteAsync(op, tableRequestOptions, operationContext);
            if (res != null && res.Result != null)
            {
                var index = (DynamicTableEntity)res.Result;
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
