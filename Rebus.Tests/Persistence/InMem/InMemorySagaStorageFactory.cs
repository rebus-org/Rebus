using Rebus.Persistence.InMem;
using Rebus.Sagas;
using Rebus.Tests.Contracts.Sagas;

namespace Rebus.Tests.Persistence.InMem
{
    public class InMemBasicLoadAndSaveAndFindOperations : BasicLoadAndSaveAndFindOperations<InMemorySagaStorageFactory> { }

    public class ConcurrencyHandling : ConcurrencyHandling<InMemorySagaStorageFactory> { }

    public class SagaIntegrationTests : SagaIntegrationTests<InMemorySagaStorageFactory> { }

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