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
                    throw new ArgumentException(string.Format("Could not find 'Metadata' document in: {0}", doc));
                }

                var sagaDataDocument = doc["Data"].AsBsonDocument;

                if (sagaDataDocument == null)
                {
                    throw new ArgumentException(string.Format("Could not find 'Data' document in: {0}", doc));
                }

                var metadata = BsonSerializer.Deserialize<Dictionary<string,string>>(metadataDocument);
                var sagaDataTypeName = metadata[SagaAuditingMetadataKeys.SagaDataType];
                var type = Type.GetType(sagaDataTypeName);

                if (type == null)
                {
                    throw new ArgumentException(
                        string.Format(
                            "Cannot deserialize saga data snapshot of type '{0}' because the corresponding .NET type could not be found!",
                            sagaDataTypeName));
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