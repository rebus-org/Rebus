using NUnit.Framework;
using Rebus.Persistence.InMem;
using Rebus.Sagas;
using Rebus.Tests.Contracts.Sagas;

namespace Rebus.Tests.Persistence.InMem
{
    [TestFixture]
    public class InMemBasicLoadAndSaveAndFindOperations : BasicLoadAndSaveAndFindOperations<InMemorySagaStorageFactory> { }

    [TestFixture]
    public class ConcurrencyHandling : ConcurrencyHandling<InMemorySagaStorageFactory> { }

    [TestFixture]
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