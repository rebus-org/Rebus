using System;
using System.Linq;
using Raven.Abstractions.Exceptions;
using Raven.Client;

namespace Rebus.RavenDb
{
    public class RavenDbSagaPersister : IStoreSagaData
    {
        const string SessionKey = "RavenDbSagaPersisterSessionKey";
        readonly IDocumentStore store;
        IMessageContext currentMessageContext;

        public RavenDbSagaPersister(IDocumentStore store)
        {
            this.store = store;
        }

        public void Save(ISagaData sagaData, string[] sagaDataPropertyPathsToIndex)
        {
            var session = GetSession();
            try
            {
                session.Store(sagaData);
                session.SaveChanges();
            }
            catch (ConcurrencyException)
            {
                throw new OptimisticLockingException(sagaData);
            }
        }

        public void Delete(ISagaData sagaData)
        {
            var session = GetSession();
            session.Delete(sagaData);
            session.SaveChanges();
        }

        public T Find<T>(string sagaDataPropertyPath, object fieldFromMessage) where T : ISagaData
        {
            var session = GetSession();

            if (sagaDataPropertyPath == "Id")
                return session.Load<T>(store.Conventions.GetTypeTagName(typeof (T)) + "/" + fieldFromMessage);

            return session.Advanced.LuceneQuery<T>()
                .WaitForNonStaleResults()
                .WhereEquals(sagaDataPropertyPath, fieldFromMessage)
                .SingleOrDefault();
        }

        internal IMessageContext CurrentMessageContext
        {
            get
            {
                if (currentMessageContext == null && !MessageContext.HasCurrent)
                    throw new InvalidOperationException("RavenDbSagaPersister can not be used outside of message context");
                
                return currentMessageContext ?? MessageContext.GetCurrent();
            }
            set { currentMessageContext = value; }
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