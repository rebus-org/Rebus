using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.WindowsAzure.Storage.Blob;
using Rebus.Auditing.Sagas;
using Rebus.AzureStorage.Sagas;
using Rebus.AzureStorage.Tests.Transport;
using Rebus.Tests.Contracts.Sagas;

namespace Rebus.AzureStorage.Tests.Sagas
{
    public class AzureStorageSagaSnapshotStorageFactory : AzureStorageFactoryBase, ISagaSnapshotStorageFactory
    {
        //private static readonly string ContainerName = $"rsss{DateTime.Now:yyyyMMddHHmmss}";
        private AzureStorageSagaSnapshotStorage _storage;
        public AzureStorageSagaSnapshotStorageFactory()
        {
            _storage = new AzureStorageSagaSnapshotStorage(StorageAccount, $"rsss{DateTime.Now:yyyyMMddHHmmssffff}");
            
        }
        public ISagaSnapshotStorage Create()
        {
            _storage.EnsureContainer();
            return _storage;
        }

        public IEnumerable<SagaDataSnapshot> GetAllSnapshots()
        {

            var allBlobs = _storage.ListAllBlobs().Cast<CloudBlockBlob>()
                .Select(b=>new
                {
                    
                    Parts = b.Name.Split('/')
                
                })
                .Select(b=>new
                {
                    Id = Guid.ParseExact(b.Parts[0],"N"),
                    Revision = Int32.Parse(b.Parts[1]),
                    Part = b.Parts[2],

                })
                .GroupBy(b=>new {b.Id, b.Revision})
                .Select(g=> new SagaDataSnapshot
                {
                    SagaData = _storage.GetSagaData(g.Key.Id, g.Key.Revision),
                    Metadata = _storage.GetSagaMetaData(g.Key.Id, g.Key.Revision)
                })
                .ToList();
                
            
            return allBlobs;
        }

    }
}