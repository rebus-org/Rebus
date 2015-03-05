using NUnit.Framework;
using Rebus.Persistence.InMem;
using Rebus.Sagas;
using Rebus.Tests.Contracts.Sagas;

namespace Rebus.Tests.Persistence.InMem
{
    [TestFixture]
    public class BasicOperations : BasicOperations<InMemorySagaStorageFactory> { }

    [TestFixture]
    public class ConcurrencyHandling : ConcurrencyHandling<InMemorySagaStorageFactory> { }

    public class InMemorySagaStorageFactory : ISagaStorageFactory
    {
        public ISagaStorage GetSagaStorage()
        {
            return new InMemorySagaStorage();
        }

        public void Cleanup()
        {
        }
    }
}