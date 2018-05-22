using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace Rebus.Sagas.Exclusive
{
    class HandleSagaExlusiveLockInConcurrentDictionary : IHandleSagaExlusiveLock
    {
        readonly ConcurrentDictionary<string, string> _locks = new ConcurrentDictionary<string, string>();
        public Task<bool> AquireLockAsync(string key)
        {
            return Task.FromResult(_locks.TryAdd(key, "dummy"));
        }

        public Task<bool> ReleaseLockAsync(string key)
        {
            return Task.FromResult(_locks.TryRemove(key, out var dummy));
        }
    }
}