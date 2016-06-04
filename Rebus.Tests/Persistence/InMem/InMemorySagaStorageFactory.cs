using Rebus.Persistence.InMem;
using Rebus.Sagas;
using Rebus.Tests.Contracts.Sagas;

namespace Rebus.Tests.Persistence.InMem
{
    public class InMemorySagaStorageFactory : ISagaStorageFactory
    {
        public ISagaStorage GetSagaStorage()
        {
            return new InMemorySagaStorage();
        }

        public void CleanUp()
        {
        }
    }
}