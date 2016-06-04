using NUnit.Framework;
using Rebus.Tests.Contracts.Timeouts;

namespace Rebus.Tests.Persistence.InMem
{
    [TestFixture]
    public class BasicStoreAndRetrieveOperations : BasicStoreAndRetrieveOperations<InMemoryTimeoutManagerFactory> { }
}