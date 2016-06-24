using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Rebus.DataBus;
using Rebus.Extensions;
using Rebus.Logging;
using Rebus.Time;

namespace Rebus.AzureStorage.DataBus
{
    /// <summary>
    /// Implementation of <see cref="IDataBusStorage"/> that uses Azure blobs to store data
    /// </summary>
    public class AzureBlobsDataBusStorage : IDataBusStorage
    {
        readonly CloudBlobClient _client;
        readonly string _containerName;
        readonly ILog _log;

        bool _containerInitialized;

        /// <summary>
        /// Creates the data bus storage
        /// </summary>
        public AzureBlobsDataBusStorage(CloudStorageAccount storageAccount, string containerName, IRebusLoggerFactory loggerFactory)
        {
            if (storageAccount == null) throw new ArgumentNullException(nameof(storageAccount));
            if (containerName == null) throw new ArgumentNullException(nameof(containerName));
            if (loggerFactory == null) throw new ArgumentNullException(nameof(loggerFactory));
            _containerName = containerName.ToLowerInvariant();
            _client = storageAccount.CreateCloudBlobClient();
            _log = loggerFactory.GetCurrentClassLogger();
        }

        /// <summary>
        /// Saves the data from the given source stream under the given ID
        /// </summary>
        public async Task Save(string id, Stream source, Dictionary<string, string> metadata = null)
        {
            var container = _client.GetContainerReference(_containerName);

            if (!_containerInitialized)
            {
                if (!await container.ExistsAsync())
                {
                    _log.Info("Container {0} does not exist - will create it now", _containerName);
                    await container.CreateIfNotExistsAsync();
                }
                _containerInitialized = true;
            }

            var blobName = GetBlobName(id);

            try
            {
                var blob = container.GetBlockBlobReference(blobName);

                var standardMetadata = new Dictionary<string, string>
                {
                    {MetadataKeys.SaveTime, RebusTime.Now.ToString("O")}
                };

                var metadataToWrite = standardMetadata
                    .MergedWith(metadata ?? new Dictionary<string, string>());

                foreach (var kvp in metadataToWrite)
                {
                    blob.Metadata[kvp.Key] = kvp.Value;
                }

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
        public async Task<Stream> Read(string id)
        {
            var blobName = GetBlobName(id);
            try
            {
                var container = _client.GetContainerReference(_containerName);

                var blob = await container.GetBlobReferenceFromServerAsync(blobName);

                await UpdateLastReadTime(blob);

                return blob.OpenRead();
            }
            catch (StorageException exception) when (exception.IsStatus(HttpStatusCode.NotFound))
            {
                throw new ArgumentException($"Could not find blob named '{blobName}' in the '{_containerName}' container", exception);
            }
        }

        static async Task UpdateLastReadTime(ICloudBlob blob)
        {
            blob.Metadata[MetadataKeys.ReadTime] = RebusTime.Now.ToString("O");

            await blob.SetMetadataAsync();
        }

        /// <summary>
        /// Loads the metadata stored with the given ID
        /// </summary>
        public async Task<Dictionary<string, string>> ReadMetadata(string id)
        {
            var blobName = GetBlobName(id);
            try
            {
                var container = _client.GetContainerReference(_containerName);
                var blob = await container.GetBlobReferenceFromServerAsync(blobName);

                var metadata = new Dictionary<string, string>(blob.Metadata)
                {
                    [MetadataKeys.Length] = blob.Properties.Length.ToString()
                };

                return metadata;
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