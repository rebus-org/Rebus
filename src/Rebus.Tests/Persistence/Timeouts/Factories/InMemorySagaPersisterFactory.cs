using Rebus.Persistence.InMemory;
using Rebus.Tests.Persistence.Sagas;
using Rebus.Timeout;

namespace Rebus.Tests.Persistence.Timeouts.Factories
{
    public class InMemoryTimeoutStorageFactory : ITimeoutStorageFactory
    {
        public IStoreTimeouts CreateStore()
        {
            return new InMemoryTimeoutStorage();
        }

        public void Dispose()
        {
        }
    }
}