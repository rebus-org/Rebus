using NUnit.Framework;
using Rebus.Config;
using Rebus.Sagas;
using Rebus.Sagas.Exclusive;

namespace Rebus.Tests.Sagas;

[TestFixture]
public class TestSagaInstanceLocking : TestSagaInstanceLockingBase
{
    protected override void EnforceExclusiveAccess(StandardConfigurer<ISagaStorage> configurer)
    {
        // Use the default semaphore slim implementation
        configurer.EnforceExclusiveAccess();
    }
}