using System.Threading.Tasks;
using Rebus.Sagas.Locking;

namespace Rebus.Persistence.SqlServer
{
    public class SqlServerPessimisticLocker : IPessimisticLocker
    {
        public Task<bool> TryAcquire(string lockId)
        {
            throw new System.NotImplementedException();
        }

        public Task Release(string lockid)
        {
            throw new System.NotImplementedException();
        }
    }
}