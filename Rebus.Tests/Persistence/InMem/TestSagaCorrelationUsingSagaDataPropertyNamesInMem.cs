using NUnit.Framework;
using Rebus.Tests.Contracts.Sagas;

namespace Rebus.Tests.Persistence.InMem
{
    [TestFixture]
    public class TestSagaCorrelationUsingSagaDataPropertyNamesInMem : TestSagaCorrelation<InMemorySagaStorageFactory>
    {
        protected override void SetUp()
        {
            base.SetUp();
            CorrelateUsingSagaDataPropertyNames = true;
        }
    }
}