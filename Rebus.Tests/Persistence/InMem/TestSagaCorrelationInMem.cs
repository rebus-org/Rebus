using NUnit.Framework;
using Rebus.Tests.Persistence.InMem;

namespace Rebus.Tests.Integration
{
    [TestFixture]
    public class TestSagaCorrelationInMem : TestSagaCorrelation<InMemorySagaStorageFactory> { }
}