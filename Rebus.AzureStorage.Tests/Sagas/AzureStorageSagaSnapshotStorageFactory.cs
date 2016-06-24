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
        readonly AzureStorageSagaSnapshotStorage _storage;

        public AzureStorageSagaSnapshotStorageFactory()
        {
            _storage = new AzureStorageSagaSnapshotStorage(StorageAccount, new ConsoleLoggerFactory(false), $"RebusSagaSnapshotStorageTestContainer{DateTime.Now:yyyyMMddHHmmss}");
        }

        public ISagaSnapshotStorage Create()
        {
            _storage.DropAndRecreateContainer();
            _storage.EnsureContainerExists();
            return _storage;
        }

        public IEnumerable<SagaDataSnapshot> GetAllSnapshots()
        {
            var allBlobs = _storage.ListAllBlobs().Cast<CloudBlockBlob>()
                .Select(b => new
                {
                    Parts = b.Name.Split('/')
                })
                .Where(x => x.Parts.Length == 3)
                .Select(b =>
                {
                    var guid = Guid.Parse(b.Parts[0]);
                    var revision = int.Parse(b.Parts[1]);
                    var part = b.Parts[2];
                    return new
                    {
                        Id = guid,
                        Revision = revision,
                        Part = part,
                    };
                })
                .GroupBy(b => new { b.Id, b.Revision })
                .Select(g => new SagaDataSnapshot
                {
                    SagaData = _storage.GetSagaData(g.Key.Id, g.Key.Revision),
                    Metadata = _storage.GetSagaMetaData(g.Key.Id, g.Key.Revision)
                })
                .ToList();

            return allBlobs;
        }
    }
}