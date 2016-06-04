using NUnit.Framework;
using Rebus.Tests.Contracts.Sagas;

namespace Rebus.Tests.Persistence.InMem
{
    [TestFixture]
    public class ConcurrencyHandling : ConcurrencyHandling<InMemorySagaStorageFactory> { }
}