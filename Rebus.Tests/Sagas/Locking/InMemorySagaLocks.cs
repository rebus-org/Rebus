using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace Rebus.Tests.Sagas.Locking
{
    public class InMemorySagaLocks
    {
        readonly ConcurrentDictionary<string, object> _locks = new ConcurrentDictionary<string, object>();

        public async Task<bool> TryGrab(string lockId)
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
    }
}