using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Raven.Client;
using Raven.Imports.Newtonsoft.Json;
using Rebus.Exceptions;
using Rebus.Sagas;
// ReSharper disable UnusedMember.Local
// ReSharper disable AutoPropertyCanBeMadeGetOnly.Local

namespace Rebus.RavenDb.Sagas
{
    /// <summary>
    /// Implementation of <see cref="ISagaStorage"/> that uses RavenDB to store sagas
    /// </summary>
    public class RavenDbSagaStorage : ISagaStorage
    {
        readonly IDocumentStore _documentStore;
        readonly string _sagaDataIdPropertyName = nameof(ISagaData.Id);

        /// <summary>
        /// Creates the saga storage using the given document store to store saga instances
        /// </summary>
        public RavenDbSagaStorage(IDocumentStore documentStore)
        {
            if (documentStore == null) throw new ArgumentNullException(nameof(documentStore));
            _documentStore = documentStore;
        }

        /// <summary>
        /// Finds an already-existing instance of the given saga data type that has a property with the given <paramref name="propertyName"/>
        /// whose value matches <paramref name="propertyValue"/>. Returns null if no such instance could be found
        /// </summary>
        public async Task<ISagaData> Find(Type sagaDataType, string propertyName, object propertyValue)
        {
            using (var session = _documentStore.OpenAsyncSession())
            {
                string sagaDataDocumentId;

                if (propertyName == _sagaDataIdPropertyName)
                {
                    sagaDataDocumentId = SagaDataDocument.GetIdFromGuid((Guid) propertyValue);
                }
                else
                {
                    var sagaCorrelationPropertyDocumentId =
                        SagaCorrelationPropertyDocument.GetIdForCorrelationProperty(sagaDataType, propertyName,
                            propertyValue);

                    var existingSagaCorrelationPropertyDocument =
                        await session.LoadAsync<SagaCorrelationPropertyDocument>(sagaCorrelationPropertyDocumentId);

                    sagaDataDocumentId = existingSagaCorrelationPropertyDocument?.SagaDataDocumentId;
                }

                if (sagaDataDocumentId == null)
                    return null;

                var existingSagaDataDocument = await session.LoadAsync<SagaDataDocument>(sagaDataDocumentId);
                var sagaData = existingSagaDataDocument?.SagaData;

                if (!sagaDataType.IsInstanceOfType(sagaData))
                {
                    return null;
                }

                return sagaData;
            }

        }

        public async Task Insert(ISagaData sagaData, IEnumerable<ISagaCorrelationProperty> correlationProperties)
        {
            if (sagaData.Id == Guid.Empty)
            {
                throw new InvalidOperationException($"Saga data {sagaData.GetType()} has an uninitialized Id property!");
            }

            if (sagaData.Revision != 0)
            {
                throw new InvalidOperationException($"Attempted to insert saga data with ID {sagaData.Id} and revision {sagaData.Revision}, but revision must be 0 on first insert!");
            }

            using (var session = _documentStore.OpenAsyncSession())
            {
                session.Advanced.UseOptimisticConcurrency = true;

                var sagaDataDocumentId = SagaDataDocument.GetIdFromGuid(sagaData.Id);

                var existingSagaDataDocument = await session.LoadAsync<SagaDataDocument>(sagaDataDocumentId);

                if (existingSagaDataDocument != null)
                {
                    throw new ConcurrencyException("Cannot insert document with an id that already exists");
                }

                var sagaDataDocument = new SagaDataDocument(sagaData);
                await session.StoreAsync(sagaDataDocument, sagaDataDocumentId);

                var correlationPropertyDocumentIds = await SaveCorrelationProperties(session, sagaData, correlationProperties, sagaDataDocumentId);
                sagaDataDocument.SagaCorrelationPropertyDocumentIds = correlationPropertyDocumentIds;

                await session.SaveChangesAsync();
            }
        }

        public async Task Update(ISagaData sagaData, IEnumerable<ISagaCorrelationProperty> correlationProperties)
        {
            using (var session = _documentStore.OpenAsyncSession())
            {
                session.Advanced.UseOptimisticConcurrency = true;

                var documentId = SagaDataDocument.GetIdFromGuid(sagaData.Id);
                var existingSagaData = await session.LoadAsync<SagaDataDocument>(documentId);

                if (existingSagaData == null)
                {
                    throw new ConcurrencyException("Cannot update saga that does not exist");
                }

                sagaData.Revision++;
                existingSagaData.SagaData = sagaData;
            
                //add the new saga correlation documents
                var correlationPropertyDocumentIds = await SaveCorrelationProperties(session, sagaData, correlationProperties, existingSagaData.Id);

                var oldCorrelationPropertyDocumentIdsNotPresentInNew = existingSagaData
                    .SagaCorrelationPropertyDocumentIds
                    .Where(sc => !correlationPropertyDocumentIds.Contains(sc));

                await DeleteCorrelationProperties(oldCorrelationPropertyDocumentIdsNotPresentInNew, session);

                existingSagaData.SagaCorrelationPropertyDocumentIds = correlationPropertyDocumentIds;

                await session.SaveChangesAsync();
            }
        }

        public async Task Delete(ISagaData sagaData)
        {
            using (var session = _documentStore.OpenAsyncSession())
            {
                session.Advanced.UseOptimisticConcurrency = true;

                var documentId = SagaDataDocument.GetIdFromGuid(sagaData.Id);
                var existingSagaData = await session.LoadAsync<SagaDataDocument>(documentId);

                if (existingSagaData == null)
                {
                    throw new ConcurrencyException("Cannot delete saga that does not exist");
                }

                await DeleteCorrelationPropertyDataForSaga(existingSagaData, session);

                session.Delete(existingSagaData);

                await session.SaveChangesAsync();
            }
        }

        async Task DeleteCorrelationPropertyDataForSaga(SagaDataDocument sagaDataDocument, IAsyncDocumentSession session)
        {
            await DeleteCorrelationProperties(sagaDataDocument.SagaCorrelationPropertyDocumentIds, session);
        }

        async Task DeleteCorrelationProperties(IEnumerable<string> correlationPropertyIds, IAsyncDocumentSession session)
        {
            var existingSagaCorrelationPropertyDocuments =
                await session.LoadAsync<SagaCorrelationPropertyDocument>(
                    correlationPropertyIds);

            //delete the existing saga correlation documents
            foreach (var existingSagaCorrelationPropertyDocument in existingSagaCorrelationPropertyDocuments)
            {
                session.Delete(existingSagaCorrelationPropertyDocument);
            }
        }

        async Task<IEnumerable<string>> SaveCorrelationProperties(IAsyncDocumentSession session, ISagaData sagaData, IEnumerable<ISagaCorrelationProperty> correlationProperties, string sagaDataDocumentId)
        {
            var documentIds = new List<string>();

            foreach (var correlationProperty in correlationProperties)
            {
                var propertyName = correlationProperty.PropertyName;
                var value = sagaData.GetType().GetProperty(propertyName).GetValue(sagaData).ToString();

                var documentId = SagaCorrelationPropertyDocument.GetIdForCorrelationProperty(correlationProperty.SagaDataType, propertyName,
                    value);

                var existingSagaCorrelationPropertyDocument =
                    await session.LoadAsync<SagaCorrelationPropertyDocument>(documentId);

                if (existingSagaCorrelationPropertyDocument != null)
                {
                    if (existingSagaCorrelationPropertyDocument.SagaDataDocumentId != sagaDataDocumentId)
                    {
                        throw new ConcurrencyException(
                            $"Could not save correlation properties. The following correlation property already exists with the same value for another saga: {propertyName} - {value}");
                    }
                }
                else
                {
                    var sagaCorrelationPropertyDocument = new SagaCorrelationPropertyDocument(correlationProperty.SagaDataType, propertyName, value, sagaDataDocumentId);

                    await session.StoreAsync(sagaCorrelationPropertyDocument, documentId);
                }
                
                documentIds.Add(documentId);
            }

            return documentIds;
        }

        public class SagaDataDocument
        {
            [JsonConstructor]
            SagaDataDocument()
            {
            }

            public SagaDataDocument(ISagaData sagaData)
            {
                SagaData = sagaData;
            }

            public string Id { get; private set; }

            public ISagaData SagaData { get; set; }

            public IEnumerable<string> SagaCorrelationPropertyDocumentIds { get; set; }

            public static string GetIdFromGuid(Guid guid)
            {
                return $"SagaDataDocuments/{guid}";
            }
        }

        public class SagaCorrelationPropertyDocument
        {
            [JsonConstructor]
            SagaCorrelationPropertyDocument()
            {
            }

            public SagaCorrelationPropertyDocument(Type sagaType, string propertyName, object value, string sagaDataDocumentId)
            {
                SagaTypeName = sagaType.Name;
                PropertyName = propertyName;
                Value = value;
                SagaDataDocumentId = sagaDataDocumentId;
            }

            public string Id { get; private set; }

            public string SagaTypeName { get; private set; }

            public string PropertyName { get; private set; }

            public object Value { get; private set; }

            public string SagaDataDocumentId { get; private set; }

            public static string GetIdForCorrelationProperty(Type sagaType, string propertyName, object value)
            {
                using (var hashAlgorithm = MD5.Create())
                {
                    var bytes = Encoding.UTF8.GetBytes($"{sagaType.Name}_{propertyName}_{value}");
                    var hash = hashAlgorithm.ComputeHash(bytes);
                    var hashString = GetHashString(hash);
                    return $"SagaCorrelationProperties/{hashString}";
                }
            }

            static string GetHashString(IEnumerable<byte> hash)
            {
                var sb = new StringBuilder();

                foreach (var b in hash)
                {
                    sb.Append(b.ToString("X2"));
                }

                return sb.ToString();
            }
        }
    }
}