using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Raven.Client;
using Raven.Imports.Newtonsoft.Json;
using Rebus.Exceptions;
using Rebus.Sagas;

namespace Rebus.RavenDb.Sagas
{
    public class RavenDbSagaStorage : ISagaStorage
    {
        private readonly IDocumentStore _documentStore;
        private readonly string _sagaDataIdPropertyName = nameof(ISagaData.Id);

        public RavenDbSagaStorage(IDocumentStore documentStore)
        {
            _documentStore = documentStore;
        }

        public async Task<ISagaData> Find(Type sagaDataType, string propertyName, object propertyValue)
        {
            using (var session = _documentStore.OpenAsyncSession())
            {
                string sagaDataDocumentId = null;

                if (propertyName == _sagaDataIdPropertyName)
                {
                    sagaDataDocumentId = SagaDataDocument.GetIdFromGuid((Guid)propertyValue);
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

                var existingSagaDataDocument =
                    await
                        session.LoadAsync<SagaDataDocument>(sagaDataDocumentId);

                return existingSagaDataDocument?.SagaData;
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
                var sagaDataDocumentId = SagaDataDocument.GetIdFromGuid(sagaData.Id);
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
                var documentId = SagaDataDocument.GetIdFromGuid(sagaData.Id);
                var existingSagaData = await session.LoadAsync<SagaDataDocument>(documentId);
                sagaData.Revision++;
                existingSagaData.SagaData = sagaData;
                
                await DeleteCorrelationPropertyDataForSaga(existingSagaData, session);

                //add the new saga correlation documents
                var correlationPropertyDocumentIds = await SaveCorrelationProperties(session, sagaData, correlationProperties, existingSagaData.Id);
                existingSagaData.SagaCorrelationPropertyDocumentIds = correlationPropertyDocumentIds;

                await session.SaveChangesAsync();
            }
        }

        public async Task Delete(ISagaData sagaData)
        {
            using (var session = _documentStore.OpenAsyncSession())
            {
                var documentId = SagaDataDocument.GetIdFromGuid(sagaData.Id);
                var existingSagaData = await session.LoadAsync<SagaDataDocument>(documentId);
                await DeleteCorrelationPropertyDataForSaga(existingSagaData, session);

                session.Delete(existingSagaData);

                await session.SaveChangesAsync();
            }
        }

        private async Task DeleteCorrelationPropertyDataForSaga(SagaDataDocument sagaDataDocument,
            IAsyncDocumentSession session)
        {
            var existingSagaCorrelationPropertyDocuments =
                await session.LoadAsync<SagaCorrelationPropertyDocument>(
                    sagaDataDocument.SagaCorrelationPropertyDocumentIds);

            //delete the existing saga correlation documents
            foreach (var existingSagaCorrelationPropertyDocument in existingSagaCorrelationPropertyDocuments)
            {
                session.Delete(existingSagaCorrelationPropertyDocument);
            }
        }

        private async Task<IEnumerable<string>> SaveCorrelationProperties(IAsyncDocumentSession session,
            ISagaData sagaData, IEnumerable<ISagaCorrelationProperty> correlationProperties, string sagaDataDocumentId)
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
                    throw new ConcurrencyException(
                        $"Could not save correlation properties. The following correlation property already exists with the same value for another saga: {propertyName} - {value}");

                documentIds.Add(documentId);

                var sagaCorrelationPropertyDocument = new SagaCorrelationPropertyDocument(correlationProperty.SagaDataType, propertyName,
                    value, sagaDataDocumentId);

                await session.StoreAsync(sagaCorrelationPropertyDocument, documentId);
            }

            return documentIds;
        }

        public class SagaDataDocument
        {
            [JsonConstructor]
            private SagaDataDocument()
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
            private SagaCorrelationPropertyDocument()
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

                var hashAlgorithm = MD5.Create();
                var hash = hashAlgorithm.ComputeHash(Encoding.UTF8.GetBytes($"{sagaType.Name}_{propertyName}_{value}"));
                var hashString = GetHashString(hash);
                return $"SagaCorrelationProperties/{hashString}";
            }


            private static string GetHashString(byte[] hash)
            {
                var sb = new StringBuilder();
                foreach (var b in hash)
                    sb.Append(b.ToString("X2"));

                return sb.ToString();
            }
        }
    }
}