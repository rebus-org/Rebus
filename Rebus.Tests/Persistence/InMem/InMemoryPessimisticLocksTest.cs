using NUnit.Framework;
using Rebus.Tests.Contracts.Locks;

namespace Rebus.Tests.Persistence.InMem
{
    [TestFixture]
    public class InMemoryPessimisticLocksTest : PessimisticLocksTest<InMemoryPessimisticLockerFactory> { }
}