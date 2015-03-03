using NUnit.Framework;
using Rebus2.Persistence.InMem;
using Rebus2.Sagas;

namespace Rebus.Tests.Contracts.Sagas
{
    [TestFixture]
    public class TestInMemorySagaStorage : ISagaStorageFactory
    {
        public class BasicOperations : BasicOperations<TestInMemorySagaStorage> { }

        public class ConcurrencyHandling : ConcurrencyHandling<TestInMemorySagaStorage> { }

        public ISagaStorage GetSagaStorage()
        {
            return new InMemorySagaStorage();
        }

        public void Cleanup()
        {
        }
    }
}