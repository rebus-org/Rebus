using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Rebus.ExclusiveLocks;
using Rebus.Tests.Contracts;

namespace Rebus.Tests.ExclusiveLocks;

[TestFixture]
public class TestConcurrentDictionaryExclusiveAccessLock : FixtureBase
{
    [Test]
    public async Task TestLockFunctions()
    {
        // Create a locker and use a 10ms to 20ms delay
        var locker = new ConcurrentDictionaryExclusiveAccessLock();

        // Try the check function which will return false initially
        const string lockName = "my_lock";
        var result = await locker.IsLockAcquiredAsync(lockName, CancellationToken.None);
        Assert.That(result, Is.False);

        // Get the lock once
        result = await locker.AcquireLockAsync(lockName, CancellationToken.None);
        Assert.That(result, Is.True);

        // Try to get it again, and it should fail
        result = await locker.AcquireLockAsync(lockName, CancellationToken.None);
        Assert.That(result, Is.False);

        // Try the check function
        result = await locker.IsLockAcquiredAsync(lockName, CancellationToken.None);
        Assert.That(result, Is.True);

        // Now release the lock
        await locker.ReleaseLockAsync(lockName);

        // Try the check function and it should be false now
        result = await locker.IsLockAcquiredAsync(lockName, CancellationToken.None);
        Assert.That(result, Is.False);
    }
}