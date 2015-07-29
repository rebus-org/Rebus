using NUnit.Framework;
using Rebus.Persistence.InMem;
using Rebus.Tests.Contracts.Timeouts;
using Rebus.Timeouts;

namespace Rebus.Tests.Persistence.InMem
{
    [TestFixture]
    public class BasicStoreAndRetrieveOperations : BasicStoreAndRetrieveOperations<InMemoryTimeoutManagerFactory> { }

    public class InMemoryTimeoutManagerFactory : ITimeoutManagerFactory
    {
        public ITimeoutManager Create()
        {
            return new InMemoryTimeoutManager();
        }

        public void Cleanup()
        {
        }
    }
}