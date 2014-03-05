using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Ponder;
using Raven.Abstractions.Exceptions;
using Raven.Client;
using Raven.Client.Exceptions;
using Raven.Json.Linq;

namespace Rebus.RavenDb
{
    public class RavenDbSagaPersister : IStoreSagaData
    {
        const string SessionKey = "RavenDbSagaPersisterSessionKey";
        const string MetaDataKey = "RebusUniqueValues";
        readonly IDocumentStore store;

        public RavenDbSagaPersister(IDocumentStore store)
        {
            this.store = store;
        }

        public void Insert(ISagaData sagaData, string[] sagaDataPropertyPathsToIndex)
        {
            Try(sagaData, session =>
            {
                session.Store(sagaData);
                AddUniqueness(session, sagaData, sagaDataPropertyPathsToIndex);
            });
        }

        public void Update(ISagaData sagaData, string[] sagaDataPropertyPathsToIndex)
        {
            Try(sagaData, session => RenewUniqueness(session, sagaData));
        }

        public void Delete(ISagaData sagaData)
        {
            Try(sagaData, session =>
            {
                RemoveUniqueness(session, sagaData);
                session.Delete(sagaData);
            });
        }

        public T Find<T>(string sagaDataPropertyPath, object fieldFromMessage) where T : class, ISagaData
        {
            var session = GetSession();

            if (sagaDataPropertyPath == "Id")
                return session.Load<T>(session.Advanced.DocumentStore.Conventions.GetTypeTagName(typeof(T)) + "/" + fieldFromMessage);
            
            var idForUniqueProperty = GetIdForUniqueProperty(typeof(T), sagaDataPropertyPath, fieldFromMessage);
            var property = session.Include<UniqueSagaProperty>(x => x.SagaId)
                .Load(idForUniqueProperty);

            return property != null ? session.Load<T>(property.SagaId) : null;
        }

        static void AddUniqueness(IDocumentSession session, ISagaData sagaData, IEnumerable<string> sagaDataPropertyPathsToIndex)
        {
            var uniqueSagaPropertyIds = new List<string>();
            foreach (var propertyPath in sagaDataPropertyPathsToIndex.Where(x => x != "Id"))
            {
                var value = Reflect.Value(sagaData, propertyPath);
                var id = GetIdForUniqueProperty(sagaData.GetType(), propertyPath, value);
                var sagaDocumentId = session.Advanced.GetDocumentId(sagaData);
                session.Store(new UniqueSagaProperty { Id = id, PropertyPath = propertyPath, SagaId = sagaDocumentId });
                uniqueSagaPropertyIds.Add(id);
            }

            SetUniquePropertyIdsToMetaData(session, sagaData, uniqueSagaPropertyIds);
        }

        static void RemoveUniqueness(IDocumentSession session, ISagaData sagaData)
        {
            var ids = GetUniquePropertyIdsFromMetaData(session, sagaData);

            var uniqueSagaProperties = session.Load<UniqueSagaProperty>(ids);
            foreach (var uniqueSagaProperty in uniqueSagaProperties)
            {
                session.Delete(uniqueSagaProperty);
            }
            
            SetUniquePropertyIdsToMetaData(session, sagaData, null);
        }

        static void RenewUniqueness(IDocumentSession session, ISagaData sagaData)
        {
            var ids = GetUniquePropertyIdsFromMetaData(session, sagaData);
            var uniqueSagaProperties = session.Load<UniqueSagaProperty>(ids);
            var uniqueSagaPropertyIds = new List<string>();
            foreach (var uniqueSagaProperty in uniqueSagaProperties)
            {
                var value = Reflect.Value(sagaData, uniqueSagaProperty.PropertyPath);
                var id = GetIdForUniqueProperty(sagaData.GetType(), uniqueSagaProperty.PropertyPath, value);
                var sagaDocumentId = session.Advanced.GetDocumentId(sagaData);

                if (id != uniqueSagaProperty.Id)
                {
                    session.Delete(uniqueSagaProperty);
                    session.Store(new UniqueSagaProperty { Id = id, PropertyPath = uniqueSagaProperty.PropertyPath, SagaId = sagaDocumentId });
                }

                uniqueSagaPropertyIds.Add(id);
            }

            SetUniquePropertyIdsToMetaData(session, sagaData, uniqueSagaPropertyIds);
        }

        static IEnumerable<string> GetUniquePropertyIdsFromMetaData(IDocumentSession session, ISagaData sagaData)
        {
            var metadata = session.Advanced.GetMetadataFor(sagaData);
            var uniqueSagaPropertiesIds = new List<string>();
            RavenJToken values;
            if (metadata.TryGetValue(MetaDataKey, out values))
            {
                var s = values.ToString();
                if (s == "") return uniqueSagaPropertiesIds;
                uniqueSagaPropertiesIds = s.Split(',').ToList();
            }
            return uniqueSagaPropertiesIds;
        }

        void Try(ISagaData sagaData, Action<IDocumentSession> action)
        {
            var session = GetSession();
            try
            {
                action(session);
                session.SaveChanges();
            }
            catch (ConcurrencyException concurrencyException)
            {
                throw new OptimisticLockingException(sagaData, concurrencyException);
            }
            catch (NonUniqueObjectException concurrencyException)
            {
                throw new OptimisticLockingException(sagaData, concurrencyException);
            }
        }

        static void SetUniquePropertyIdsToMetaData(IDocumentSession session, ISagaData sagaData, IEnumerable<string> uniqueSagaPropertiesIds)
        {
            var metadata = session.Advanced.GetMetadataFor(sagaData);
            metadata[MetaDataKey] = uniqueSagaPropertiesIds != null ? string.Join(",", uniqueSagaPropertiesIds) : "";
        }

        static string GetIdForUniqueProperty(Type sagaType, string propertyPath, object value)
        {
            //use MD5 hash to get a 16-byte hash of the string
            var provider = new MD5CryptoServiceProvider();
            var inputBytes = value != null
                ? Encoding.Default.GetBytes(value.ToString())
                : new byte[0];

            var hashBytes = provider.ComputeHash(inputBytes);
            //generate a guid from the hash:
            var hashedValue = new Guid(hashBytes);

            return string.Format("{0}/{1}/{2}", sagaType, propertyPath, hashedValue);
        }

        static IMessageContext CurrentMessageContext
        {
            get
            {
                try
                {
                    return MessageContext.GetCurrent();
                }
                catch(Exception exception)
                {
                    throw new InvalidOperationException(
                        "RavenDbSagaPersister can not be used outside of message context",
                        exception);
                }
            }
        }

        IDocumentSession GetSession()
        {
            var messageContext = CurrentMessageContext;

            object currentSession;
            if (messageContext.Items.TryGetValue(SessionKey, out currentSession))
            {
                return (IDocumentSession) currentSession;
            }

            var session = store.OpenSession();
            session.Advanced.UseOptimisticConcurrency = true;
            session.Advanced.AllowNonAuthoritativeInformation = false;
            messageContext.Disposed += session.Dispose;
            messageContext.Items.Add(SessionKey, session);
            return session;
        }

        class UniqueSagaProperty
        {
            public string Id { get; set; }
            public string PropertyPath { get; set; }
            public string SagaId { get; set; }
        }
    }
}