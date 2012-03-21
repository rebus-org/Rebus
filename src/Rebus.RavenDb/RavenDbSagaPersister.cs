using System.Linq;
using System.Threading;
using Raven.Abstractions.Exceptions;
using Raven.Client;

namespace Rebus.RavenDb
{
    public class RavenDbSagaPersister : IStoreSagaData
    {
        private readonly IDocumentStore store;

        public RavenDbSagaPersister(IDocumentStore store)
        {
            this.store = store;
        }

        public void Save(ISagaData sagaData, string[] sagaDataPropertyPathsToIndex)
        {
            using (var session = store.OpenSession())
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
        }

        public void Delete(ISagaData sagaData)
        {
            using (var session = store.OpenSession())
            {
                session.Advanced.DatabaseCommands.Delete(store.Conventions.DocumentKeyGenerator(sagaData), null);
                session.SaveChanges();
            }
        }

        public T Find<T>(string sagaDataPropertyPath, object fieldFromMessage) where T : ISagaData
        {
            using (var session = store.OpenSession())
            {
                session.Advanced.AllowNonAuthoritativeInformation = false;

                if (sagaDataPropertyPath == "Id")
                    return session.Load<T>(store.Conventions.GetTypeTagName(typeof(T)) + "/" + fieldFromMessage);

                return session.Advanced.LuceneQuery<T>()
                    .WaitForNonStaleResults()
                    .WhereEquals(sagaDataPropertyPath, fieldFromMessage)
                    .SingleOrDefault();
            }
        }
    }
}