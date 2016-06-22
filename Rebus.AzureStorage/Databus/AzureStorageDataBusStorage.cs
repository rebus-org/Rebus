using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage;
using Newtonsoft.Json;
using Rebus.DataBus;

namespace Rebus.AzureStorage.Databus
{
    public class AzureStorageDataBusStorage : IDataBusStorage
    {
        private readonly CloudStorageAccount _cloudStorageAccount;
        private readonly string _containerName;



        public void EnsureCreated()
        {
            var client = _cloudStorageAccount.CreateCloudBlobClient();
            var container = client.GetContainerReference(_containerName);
            container.CreateIfNotExists();

        }

        public AzureStorageDataBusStorage(CloudStorageAccount cloudStorageAccount, string containerName = "RebusDataBusData")
        {
            _cloudStorageAccount = cloudStorageAccount;
            _containerName = containerName.ToLowerInvariant();
            EnsureCreated();
        }

        public async Task Save(string id, Stream source)
        {
            var client = _cloudStorageAccount.CreateCloudBlobClient();
            var container = client.GetContainerReference(_containerName);
            var dataBlob = container.GetBlockBlobReference(id);
            await dataBlob.UploadFromStreamAsync(source);
        }

        public Stream Read(string id)
        {
            var client = _cloudStorageAccount.CreateCloudBlobClient();
            var container = client.GetContainerReference(_containerName);
            var dataBlob = container.GetBlockBlobReference(id);
            if (!dataBlob.Exists())
            {
                throw new ArgumentException($"Could not data for ID {id}");
            }
            var ms = new MemoryStream();
            dataBlob.DownloadToStream(ms);
            ms.Position = 0;
            return ms;
        }
    }
}