using System;
using System.Collections.Generic;
using System.Linq;
using Raven.Abstractions.Exceptions;
using Raven.Client;
using Raven.Client.Exceptions;
using Rebus.Extensions;

namespace Rebus.RavenDb
{
    public class RavenDbSagaPersister : IStoreSagaData
    {
        const string SessionKey = "RavenDbSagaPersisterSessionKey";
        readonly IDocumentStore store;

        public RavenDbSagaPersister(IDocumentStore store)
        {
            this.store = store;
        }

        public void Insert(ISagaData sagaData, string[] sagaDataPropertyPathsToIndex)
        {
            var session = GetSession();
            try
            {
                session.Store(sagaData);
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

        public void Update(ISagaData sagaData, string[] sagaDataPropertyPathsToIndex)
        {
            var session = GetSession();
            try
            {
                session.SaveChanges();
            }
            catch (ConcurrencyException concurrencyException)
            {
                throw new OptimisticLockingException(sagaData, concurrencyException);
            }
        }

        public void Delete(ISagaData sagaData)
        {
            var session = GetSession();
            session.Delete(sagaData);
            session.SaveChanges();
        }

        public IEnumerable<T> Find<T>(string sagaDataPropertyPath, object fieldFromMessage) where T : class, ISagaData
        {
            var session = GetSession();

            if (sagaDataPropertyPath == "Id")
            {
                var sagaData = session.Load<T>(store.Conventions.GetTypeTagName(typeof (T)) + "/" + fieldFromMessage);
                return sagaData != null ? sagaData.AsEnumerable() : Enumerable.Empty<T>();
            }

            return session.Advanced.LuceneQuery<T>()
                .WaitForNonStaleResults()
                .WhereEquals(sagaDataPropertyPath, fieldFromMessage)
                .ToList();
        }

        IMessageContext CurrentMessageContext
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
    }
}