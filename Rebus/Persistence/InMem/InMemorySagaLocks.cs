using System.Collections.Concurrent;
using System.Threading.Tasks;
using Rebus.Sagas.Locking;

namespace Rebus.Persistence.InMem
{
    public class InMemorySagaLocks : IPessimisticLock
    {
        readonly ConcurrentDictionary<string, object> _locks = new ConcurrentDictionary<string, object>();

        public async Task<bool> TryAcquire(string lockId)
        {
            var result = _locks.TryAdd(lockId, new object());
            if (result)
            {
            }
            return result;
        }

        public async Task Release(string lockId)
        {
            object dummy;
            _locks.TryRemove(lockId, out dummy);
        }

        public int Count => _locks.Count;
    }
}