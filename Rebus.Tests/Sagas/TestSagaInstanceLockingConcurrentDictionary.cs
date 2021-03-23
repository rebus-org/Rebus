using NUnit.Framework;
using Rebus.Config;
using Rebus.Sagas;
using Rebus.Sagas.Exclusive;

namespace Rebus.Tests.Sagas
{
    [TestFixture]
    public class TestSagaInstanceLockingConcurrentDictionary : TestSagaInstanceLockingBase
    {
        protected override void EnforceExclusiveAccess(StandardConfigurer<ISagaStorage> configurer)
        {
            // Use the concurrent dictionary so we can test using a custom locker. Use a short sleep
            // though for testing
            configurer.EnforceExclusiveAccessViaLocker();
        }
    }
}
