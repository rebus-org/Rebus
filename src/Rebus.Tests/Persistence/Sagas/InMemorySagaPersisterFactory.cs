using Rebus.Persistence.InMemory;

namespace Rebus.Tests.Persistence.Sagas
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