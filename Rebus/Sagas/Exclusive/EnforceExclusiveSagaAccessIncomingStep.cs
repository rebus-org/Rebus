using System;
using System.Threading;
using System.Threading.Tasks;
using Rebus.ExclusiveLocks;
using Rebus.Pipeline;

namespace Rebus.Sagas.Exclusive;

[StepDocumentation("Enforces exclusive access to saga data in the rest of the pipeline by acquiring locks for the relevant correlation properties.")]
sealed class EnforceExclusiveSagaAccessIncomingStep : EnforceExclusiveSagaAccessIncomingStepBase
{
    readonly IExclusiveAccessLock _lockHandler;
    readonly string _lockPrefix;
    private readonly TimeSpan _lockSleepMinDelay;
    private readonly TimeSpan _lockSleepMaxDelay;
    private readonly Random _random;

    public EnforceExclusiveSagaAccessIncomingStep(IExclusiveAccessLock lockHandler, int maxLockBuckets,
        string lockPrefix, CancellationToken cancellationToken, TimeSpan? lockSleepMinDelay = null,
        TimeSpan? lockSleepMaxDelay = null)
        : base(maxLockBuckets, cancellationToken)
    {
        _lockHandler = lockHandler;
        _lockPrefix = lockPrefix;
        _lockSleepMinDelay = lockSleepMinDelay ?? TimeSpan.FromMilliseconds(10);
        _lockSleepMaxDelay = lockSleepMaxDelay ?? TimeSpan.FromMilliseconds(20);
        _random = new Random(DateTime.Now.GetHashCode());
    }

    protected override async Task<bool> AcquireLockAsync(int lockId)
    {
        // We are done if we can get the lock
        if (await _lockHandler.AcquireLockAsync(LockKey(lockId), _cancellationToken).ConfigureAwait(false))
        {
            return true;
        }

        // If we did not get the lock, we need to sleep and jitter the sleep period to avoid all
        // the locked threads waking up at the same time.
        var sleepRange = _lockSleepMaxDelay.TotalMilliseconds - _lockSleepMinDelay.TotalMilliseconds;
        var sleepTime = _lockSleepMinDelay + TimeSpan.FromMilliseconds(_random.NextDouble() * sleepRange);
        await Task.Delay(sleepTime, _cancellationToken).ConfigureAwait(false);
        return false;
    }

    protected override Task<bool> ReleaseLockAsync(int lockId)
    {
        return _lockHandler.ReleaseLockAsync(LockKey(lockId));
    }

    string LockKey(int lockId) => $"{_lockPrefix}{lockId}";

    public override string ToString() => $"EnforceExclusiveSagaAccessIncomingStep({_maxLockBuckets})";
}