using Rebus.Persistence.InMemory;

namespace Rebus.Tests.Persistence.Sagas.Factories
{
    public class InMemorySagaPersisterFactory : ISagaPersisterFactory
    {
        public IStoreSagaData CreatePersister()
        {
            return new InMemorySagaPersister();
        }

        public void Dispose()
        {
        }
    }
}