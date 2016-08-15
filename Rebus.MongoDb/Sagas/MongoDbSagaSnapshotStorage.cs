using System.Collections.Generic;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;
using Rebus.Auditing.Sagas;
using Rebus.Sagas;

namespace Rebus.MongoDb.Sagas
{
    /// <summary>
    /// Implementation of <see cref="ISagaSnapshotStorage"/> that uses MongoDB to do its thing
    /// </summary>
    public class MongoDbSagaSnapshotStorage : ISagaSnapshotStorage
    {
        readonly IMongoCollection<BsonDocument> _snapshots;

        /// <summary>
        /// Constructs the snapshot storage
        /// </summary>
        public MongoDbSagaSnapshotStorage(IMongoDatabase mongoDatabase, string collectionName)
        {
            _snapshots = mongoDatabase.GetCollection<BsonDocument>(collectionName);
        }

        /// <summary>
        /// Saves a snapshot of the given saga data
        /// </summary>
        public async Task Save(ISagaData sagaData, Dictionary<string, string> sagaAuditMetadata)
        {
            var document = new BsonDocument
            {
                {"_id", new {Id = sagaData.Id, Revision = sagaData.Revision}.ToBsonDocument()},
                {"Metadata", sagaAuditMetadata.ToBsonDocument()},
                {"Data", sagaData.ToBsonDocument()}
            };

            await _snapshots.InsertOneAsync(document);
        }

    }
}