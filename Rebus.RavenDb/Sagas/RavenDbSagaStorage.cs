using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Raven.Client;
using Rebus.Sagas;

namespace Rebus.RavenDb.Sagas
{
    public class RavenDbSagaStorage : ISagaStorage
    {
        readonly IDocumentStore _documentStore;

        public RavenDbSagaStorage(IDocumentStore documentStore)
        {
            _documentStore = documentStore;
        }

        public Task<ISagaData> Find(Type sagaDataType, string propertyName, object propertyValue)
        {
            throw new NotImplementedException();
        }

        public async Task Insert(ISagaData sagaData, IEnumerable<ISagaCorrelationProperty> correlationProperties)
        {
            var session = GetCurrentSession();
         
            await session.StoreAsync(sagaData);

            await SaveCorrelationProperties(session, sagaData, correlationProperties);

            await session.SaveChangesAsync();
        }

        public async Task Update(ISagaData sagaData, IEnumerable<ISagaCorrelationProperty> correlationProperties)
        {
        }

        public async Task Delete(ISagaData sagaData)
        {
            throw new NotImplementedException();
        }

        async Task SaveCorrelationProperties(IAsyncDocumentSession session, ISagaData sagaData, IEnumerable<ISagaCorrelationProperty> correlationProperties)
        {

        }

        IAsyncDocumentSession GetCurrentSession()
        {
            return _documentStore.OpenAsyncSession();
        }

        public class CorrelationProperty
        {


            public string SagaType { get; set; }
            public string PropertyName { get; set; }
            public string Value { get; set; }

            public Guid SagaId { get; set; }
        }
    }
}