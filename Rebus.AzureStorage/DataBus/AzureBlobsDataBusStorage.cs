using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Rebus.DataBus;

namespace Rebus.AzureStorage.DataBus
{
    /// <summary>
    /// Implementation of <see cref="IDataBusStorage"/> that uses Azure blobs to store data
    /// </summary>
    public class AzureBlobsDataBusStorage : IDataBusStorage
    {
        readonly CloudBlobClient _client;
        readonly string _containerName;

        bool _containerInitialized;

        /// <summary>
        /// Creates the data bus storage
        /// </summary>
        public AzureBlobsDataBusStorage(CloudStorageAccount storageAccount, string containerName)
        {
            if (storageAccount == null) throw new ArgumentNullException(nameof(storageAccount));
            if (containerName == null) throw new ArgumentNullException(nameof(containerName));
            _containerName = containerName.ToLowerInvariant();
            _client = storageAccount.CreateCloudBlobClient();
        }

        /// <summary>
        /// Saves the data from the given source stream under the given ID
        /// </summary>
        public async Task Save(string id, Stream source)
        {
            var container = _client.GetContainerReference(_containerName);

            if (!_containerInitialized)
            {
                await container.CreateIfNotExistsAsync();
                _containerInitialized = true;
            }

            var blobName = GetBlobName(id);

            try
            {
                var blob = container.GetBlockBlobReference(blobName);

                await blob.UploadFromStreamAsync(source);
            }
            catch (Exception exception)
            {
                throw new IOException($"Could not upload data to blob named '{blobName}' in the '{_containerName}' container", exception);
            }
        }

        /// <summary>
        /// Opens the data stored under the given ID for reading
        /// </summary>
        public Stream Read(string id)
        {
            var blobName = GetBlobName(id);
            try
            {
                var container = _client.GetContainerReference(_containerName);
                var blob = container.GetBlobReferenceFromServer(blobName);

                return blob.OpenRead();
            }
            catch (StorageException exception) when (exception.IsStatus(HttpStatusCode.NotFound))
            {
                throw new ArgumentException($"Could not find blob named '{blobName}' in the '{_containerName}' container", exception);
            }
        }

        static string GetBlobName(string id)
        {
            return $"data-{id.ToLowerInvariant()}.dat";
        }
    }
}