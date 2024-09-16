using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace Rebus.ExclusiveLocks;

/// <summary>
/// ConcurrentDictionary implementation of <see cref="IExclusiveAccessLock"/>
/// </summary>
sealed class ConcurrentDictionaryExclusiveAccessLock : IExclusiveAccessLock
{
    static readonly Task<bool> TrueResult = Task.FromResult(true);
    static readonly Task<bool> FalseResult = Task.FromResult(false);
    
    readonly ConcurrentDictionary<string, byte> _locks = new();

    public Task<bool> AcquireLockAsync(string key, CancellationToken cancellationToken) => _locks.TryAdd(key, 1) ? TrueResult : FalseResult;

    public Task<bool> IsLockAcquiredAsync(string key, CancellationToken cancellationToken) => Task.FromResult(_locks.TryGetValue(key, out _));

    public Task<bool> ReleaseLockAsync(string key) => _locks.TryRemove(key, out var dummy) ? TrueResult : FalseResult;
}