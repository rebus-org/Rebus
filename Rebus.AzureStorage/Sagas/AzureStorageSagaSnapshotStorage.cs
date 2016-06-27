using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage.RetryPolicies;
using Newtonsoft.Json;
using Rebus.Auditing.Sagas;
using Rebus.Logging;
using Rebus.Sagas;

namespace Rebus.AzureStorage.Sagas
{
    /// <summary>
    /// Implementation of <see cref="ISagaSnapshotStorage"/> that uses blobs to store saga data snapshots
    /// </summary>
    public class AzureStorageSagaSnapshotStorage : ISagaSnapshotStorage
    {
        static readonly JsonSerializerSettings DataSettings = new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.All };
        static readonly JsonSerializerSettings MetadataSettings = new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.None };
        static readonly Encoding TextEncoding = Encoding.UTF8;

        readonly CloudBlobContainer _container;
        readonly ILog _log;

        /// <summary>
        /// Creates the storage
        /// </summary>
        public AzureStorageSagaSnapshotStorage(CloudStorageAccount storageAccount, IRebusLoggerFactory loggerFactory, string containerName = "RebusSagaStorage")
        {
            if (storageAccount == null) throw new ArgumentNullException(nameof(storageAccount));
            if (loggerFactory == null) throw new ArgumentNullException(nameof(loggerFactory));

            _log = loggerFactory.GetCurrentClassLogger();
            _container = storageAccount.CreateCloudBlobClient()
                .GetContainerReference(containerName.ToLowerInvariant());
        }

        /// <summary>
        /// Archives the given saga data under its current ID and revision
        /// </summary>
        public async Task Save(ISagaData sagaData, Dictionary<string, string> sagaAuditMetadata)
        {
            var dataRef = $"{sagaData.Id:N}/{sagaData.Revision:0000000000}/data.json";
            var metaDataRef = $"{sagaData.Id:N}/{sagaData.Revision:0000000000}/metadata.json";
            var dataBlob = _container.GetBlockBlobReference(dataRef);
            var metaDataBlob = _container.GetBlockBlobReference(metaDataRef);
            dataBlob.Properties.ContentType = "application/json";
            metaDataBlob.Properties.ContentType = "application/json";
            await dataBlob.UploadTextAsync(JsonConvert.SerializeObject(sagaData, DataSettings), TextEncoding, DefaultAccessCondition, DefaultRequestOptions, DefaultOperationContext);
            await metaDataBlob.UploadTextAsync(JsonConvert.SerializeObject(sagaAuditMetadata, MetadataSettings), TextEncoding, DefaultAccessCondition, DefaultRequestOptions, DefaultOperationContext);
            await dataBlob.SetPropertiesAsync();
            await metaDataBlob.SetPropertiesAsync();
        }

        static OperationContext DefaultOperationContext => new OperationContext();

        static BlobRequestOptions DefaultRequestOptions => new BlobRequestOptions { RetryPolicy = new ExponentialRetry() };

        static AccessCondition DefaultAccessCondition => AccessCondition.GenerateEmptyCondition();

        /// <summary>
        /// Gets all blobs in the snapshot container
        /// </summary>
        public IEnumerable<IListBlobItem> ListAllBlobs()
        {
            return _container.ListBlobs(useFlatBlobListing: true);
        }

        /// <summary>
        /// Creates the blob container if it doesn't exist
        /// </summary>
        public void EnsureContainerExists()
        {
            if (!_container.Exists())
            {
                _log.Info("Container {0} did not exist - it will be created now", _container.Name);
                _container.CreateIfNotExists();
            }
        }

        static string GetBlobData(CloudBlockBlob cloudBlockBlob)
        {
            return cloudBlockBlob.DownloadText(TextEncoding, new AccessCondition(),
                new BlobRequestOptions { RetryPolicy = new ExponentialRetry() }, new OperationContext());
        }

        /// <summary>
        /// Loads the saga data with the given id and revision
        /// </summary>
        public ISagaData GetSagaData(Guid sagaDataId, int revision)
        {
            var dataRef = $"{sagaDataId:N}/{revision:0000000000}/data.json";
            var dataBlob = _container.GetBlockBlobReference(dataRef);
            var json = GetBlobData(dataBlob);
            return (ISagaData)JsonConvert.DeserializeObject(json, DataSettings);
        }

        /// <summary>
        /// Loads the saga metadata for the saga with the given id and revision
        /// </summary>
        public Dictionary<string, string> GetSagaMetaData(Guid sagaDataId, int revision)
        {
            var metaDataRef = $"{sagaDataId:N}/{revision:0000000000}/metadata.json";
            var metaDataBlob = _container.GetBlockBlobReference(metaDataRef);
            var json = GetBlobData(metaDataBlob);
            return JsonConvert.DeserializeObject<Dictionary<string, string>>(json, MetadataSettings);
        }

        /// <summary>
        /// Drops/recreates the snapshot container
        /// </summary>
        public void DropAndRecreateContainer()
        {
            _container.DeleteIfExists();
            _container.CreateIfNotExists();
        }
    }
}