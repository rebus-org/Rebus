using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace Rebus.Sagas.Exclusive
{
    /// <summary>
    /// Default implementation of <see cref="IExclusiveSagaAccessLock"/>
    /// </summary>
    class ConcurrentDictionaryExclusiveSagaAccessLock : IExclusiveSagaAccessLock
    {
        static readonly Task<bool> TrueResult = Task.FromResult(true);
        static readonly Task<bool> FalseResult = Task.FromResult(false);

        readonly ConcurrentDictionary<int, byte> _locks = new ConcurrentDictionary<int, byte>();

        public Task<bool> AquireLockAsync(int key, CancellationToken cancellationToken) => _locks.TryAdd(key, 1) ? TrueResult : FalseResult;

        public Task<bool> ReleaseLockAsync(int key) => _locks.TryRemove(key, out var dummy) ? TrueResult : FalseResult;
    }
}