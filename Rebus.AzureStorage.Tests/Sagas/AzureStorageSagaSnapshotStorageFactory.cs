using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.WindowsAzure.Storage.Blob;
using Rebus.Auditing.Sagas;
using Rebus.AzureStorage.Sagas;
using Rebus.AzureStorage.Tests.Transport;
using Rebus.Logging;
using Rebus.Tests.Contracts.Sagas;

namespace Rebus.AzureStorage.Tests.Sagas
{
    public class AzureStorageSagaSnapshotStorageFactory : AzureStorageFactoryBase, ISagaSnapshotStorageFactory
    {
        //private static readonly string ContainerName = $"rsss";
        private AzureStorageSagaSnapshotStorage _storage;
        public AzureStorageSagaSnapshotStorageFactory()
        {
            _storage = new AzureStorageSagaSnapshotStorage(StorageAccount, new ConsoleLoggerFactory(false),  $"RebusSagaSnapshotStorageTestContainer{DateTime.Now:yyyyMMddHHmmss}");
            
        }
        public ISagaSnapshotStorage Create()
        {
            _storage.DropAndRecreateContainer();
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
                .Where(x=>x.Parts.Length == 3)
                .Select(b=>
                {
                    var guid = Guid.Parse(b.Parts[0]);
                    var i = Int32.Parse(b.Parts[1]);
                    return new
                    {
                        Id = guid,
                        Revision = i,
                        Part = b.Parts[2],

                    };
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