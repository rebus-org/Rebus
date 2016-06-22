using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage.RetryPolicies;
using Newtonsoft.Json;
using Rebus.Auditing.Sagas;
using Rebus.Sagas;

namespace Rebus.AzureStorage.Sagas
{
    public class AzureStorageSagaSnapshotStorage : ISagaSnapshotStorage
    {
        private readonly CloudStorageAccount _cloudStorageAccount;
        private readonly string _containerName;

        static readonly JsonSerializerSettings DataSettings =
       new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.All };

        static readonly JsonSerializerSettings MetadataSettings =
            new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.None };


        public AzureStorageSagaSnapshotStorage(CloudStorageAccount account, string containerName = "RebusSagaStorage")
        {

            _cloudStorageAccount = account;
            _containerName = containerName.ToLowerInvariant();

        }

        public async Task Save(ISagaData sagaData, Dictionary<string, string> sagaAuditMetadata)
        {
            var dataRef = $"{sagaData.Id:N}/{sagaData.Revision:0000000000}/data.json";
            var metaDataRef = $"{sagaData.Id:N}/{sagaData.Revision:0000000000}/metadata.json";
            var client = _cloudStorageAccount.CreateCloudBlobClient();
            var container = client.GetContainerReference(_containerName);
            var dataBlob = container.GetBlockBlobReference(dataRef);
            var metaDataBlob = container.GetBlockBlobReference(metaDataRef);
            dataBlob.Properties.ContentType = "application/json";
            metaDataBlob.Properties.ContentType = "application/json";
            await dataBlob.UploadTextAsync(JsonConvert.SerializeObject(sagaData, DataSettings), Encoding.Unicode, new AccessCondition(), new BlobRequestOptions {RetryPolicy = new ExponentialRetry()}, new OperationContext());
            await metaDataBlob.UploadTextAsync(JsonConvert.SerializeObject(sagaAuditMetadata, MetadataSettings), Encoding.Unicode, new AccessCondition(), new BlobRequestOptions { RetryPolicy = new ExponentialRetry() }, new OperationContext());
            await dataBlob.SetPropertiesAsync();
            await metaDataBlob.SetPropertiesAsync();
        }

        public IEnumerable<IListBlobItem> ListAllBlobs()
        {
            var client = _cloudStorageAccount.CreateCloudBlobClient();
            var container = client.GetContainerReference(_containerName);
            return container.ListBlobs(useFlatBlobListing: true);
        }

        public void EnsureContainer()
        {
            var client = _cloudStorageAccount.CreateCloudBlobClient();
            var container = client.GetContainerReference(_containerName);
            container.CreateIfNotExists();
        }

        public string GetBlobData(CloudBlockBlob cloudBlockBlob)
        {
            return cloudBlockBlob.DownloadText(Encoding.Unicode, new AccessCondition(),
                new BlobRequestOptions {RetryPolicy = new ExponentialRetry()}, new OperationContext());
        }

        public ISagaData GetSagaData(Guid sagaDataId, int revision)
        {
            var dataRef = $"{sagaDataId:N}/{revision:0000000000}/data.json";
            var metaDataRef = $"{sagaDataId:N}/{revision:0000000000}/metadata.json";
            var client = _cloudStorageAccount.CreateCloudBlobClient();
            var container = client.GetContainerReference(_containerName);
            var dataBlob = container.GetBlockBlobReference(dataRef);
            var metaDataBlob = container.GetBlockBlobReference(metaDataRef);
            var json = GetBlobData(dataBlob);
            return (ISagaData)JsonConvert.DeserializeObject(json, DataSettings);
        }

        public Dictionary<string, string> GetSagaMetaData(Guid sagaDataId, int revision)
        {
            
            var metaDataRef = $"{sagaDataId:N}/{revision:0000000000}/metadata.json";
            var client = _cloudStorageAccount.CreateCloudBlobClient();
            var container = client.GetContainerReference(_containerName);
            
            var metaDataBlob = container.GetBlockBlobReference(metaDataRef);
            var json = GetBlobData(metaDataBlob);
            return JsonConvert.DeserializeObject<Dictionary<string,string>>(json, MetadataSettings);
        }

        public void DropAndRecreateContainer()
        {
            var client = _cloudStorageAccount.CreateCloudBlobClient();
            var container = client.GetContainerReference(_containerName);
            container.DeleteIfExists();
            container.CreateIfNotExists();
        }
    }
}