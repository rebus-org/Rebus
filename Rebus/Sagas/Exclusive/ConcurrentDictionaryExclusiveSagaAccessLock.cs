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
        const string DummyValue = "dummy";
        
        static readonly Task<bool> TrueResult = Task.FromResult(true);
        static readonly Task<bool> FalseResult = Task.FromResult(false);

        readonly ConcurrentDictionary<string, string> _locks = new ConcurrentDictionary<string, string>();

        public Task<bool> AquireLockAsync(string key, CancellationToken cancellationToken) => _locks.TryAdd(key, DummyValue) ? TrueResult : FalseResult;

        public Task<bool> ReleaseLockAsync(string key) => _locks.TryRemove(key, out var dummy) ? TrueResult : FalseResult;
    }
}