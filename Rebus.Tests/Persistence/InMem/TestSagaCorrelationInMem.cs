using Rebus.Tests.Contracts.Sagas;

namespace Rebus.Tests.Persistence.InMem
{
    public class TestSagaCorrelationInMem : TestSagaCorrelation<InMemorySagaStorageFactory> { }
}