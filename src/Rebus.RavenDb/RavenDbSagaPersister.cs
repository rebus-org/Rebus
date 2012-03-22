using System.Linq;
using Raven.Abstractions.Exceptions;
using Raven.Client;

namespace Rebus.RavenDb
{
    public class RavenDbSagaPersister : IStoreSagaData
    {
        private readonly IDocumentSession session;
        private readonly IDocumentStore store;

        public RavenDbSagaPersister(IDocumentStore store)
        {
            this.store = store;
            session = store.OpenSession();
            session.Advanced.UseOptimisticConcurrency = true;
            session.Advanced.AllowNonAuthoritativeInformation = false;
        }

        public void Save(ISagaData sagaData, string[] sagaDataPropertyPathsToIndex)
        {
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
            session.Delete(sagaData);
            session.SaveChanges();
        }

        public T Find<T>(string sagaDataPropertyPath, object fieldFromMessage) where T : ISagaData
        {
            if (sagaDataPropertyPath == "Id")
                return session.Load<T>(store.Conventions.GetTypeTagName(typeof (T)) + "/" + fieldFromMessage);

            return session.Advanced.LuceneQuery<T>()
                .WaitForNonStaleResults()
                .WhereEquals(sagaDataPropertyPath, fieldFromMessage)
                .SingleOrDefault();
        }
    }
}