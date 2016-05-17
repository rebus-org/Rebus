using System;
using System.Collections.Generic;
using System.Linq;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using Rebus.Auditing.Sagas;
using Rebus.MongoDb.Sagas;
using Rebus.Sagas;
using Rebus.Tests.Contracts.Sagas;

namespace Rebus.MongoDb.Tests.Sagas
{
    public class MongoDbSnapshotStorageFactory : ISagaSnapshotStorageFactory
    {
        const string CollectionName = "SagaSnaps";
        readonly IMongoDatabase _mongoDatabase;

        public MongoDbSnapshotStorageFactory()
        {
            MongoTestHelper.DropMongoDatabase();
        
            _mongoDatabase = MongoTestHelper.GetMongoDatabase();
        }

        public ISagaSnapshotStorage Create()
        {
            return new MongoDbSagaSnapshotStorage(_mongoDatabase, CollectionName);
        }

        public IEnumerable<SagaDataSnapshot> GetAllSnapshots()
        {
            var snaps = _mongoDatabase.GetCollection<BsonDocument>(CollectionName)
                .FindAsync(_ => true).Result
                .ToListAsync().Result;

            return snaps.Select(doc =>
            {
                var metadataDocument = doc["Metadata"].AsBsonDocument;

                if (metadataDocument == null)
                {
                    throw new ArgumentException($"Could not find 'Metadata' document in: {doc}");
                }

                var sagaDataDocument = doc["Data"].AsBsonDocument;

                if (sagaDataDocument == null)
                {
                    throw new ArgumentException($"Could not find 'Data' document in: {doc}");
                }

                var metadata = BsonSerializer.Deserialize<Dictionary<string,string>>(metadataDocument);
                var sagaDataTypeName = metadata[SagaAuditingMetadataKeys.SagaDataType];
                var type = Type.GetType(sagaDataTypeName);

                if (type == null)
                {
                    throw new ArgumentException(
                        $"Cannot deserialize saga data snapshot of type '{sagaDataTypeName}' because the corresponding .NET type could not be found!");
                }

                var sagaData = BsonSerializer.Deserialize(sagaDataDocument, type);

                return new SagaDataSnapshot
                {
                    Metadata = metadata,
                    SagaData = (ISagaData)sagaData,
                };
            });
        }
    }
}