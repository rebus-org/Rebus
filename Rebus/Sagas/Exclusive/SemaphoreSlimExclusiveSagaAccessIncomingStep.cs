using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

// ReSharper disable ForCanBeConvertedToForeach
#pragma warning disable 1998

namespace Rebus.Sagas.Exclusive;

class SemaphoreSlimExclusiveSagaAccessIncomingStep : EnforceExclusiveSagaAccessIncomingStepBase, IDisposable
{
    readonly SemaphoreSlim[] _locks;

    public SemaphoreSlimExclusiveSagaAccessIncomingStep(int lockBuckets, CancellationToken cancellationToken)
        : base(lockBuckets, cancellationToken)
    {
        _locks = Enumerable.Range(0, lockBuckets)
            .Select(n => new SemaphoreSlim(1, 1))
            .ToArray();
    }

    protected override async Task<bool> AcquireLockAsync(int lockId)
    {
        await _locks[lockId].WaitAsync(_cancellationToken);
        return true;
    }

    protected override async Task<bool> ReleaseLockAsync(int lockId)
    {
        _locks[lockId].Release();
        return true;
    }

    public override string ToString() => $"SemaphoreSlimExclusiveSagaAccessIncomingStep({_locks.Length})";

    bool _disposed;

    public void Dispose()
    {
        if (_disposed) return;

        try
        {
            foreach (var disposable in _locks)
            {
                disposable.Dispose();
            }
        }
        finally
        {
            _disposed = true;
        }
    }
}