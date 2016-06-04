using Rebus.Persistence.InMem;
using Rebus.Sagas.Locking;
using Rebus.Tests.Contracts.Locks;

namespace Rebus.Tests.Persistence.InMem
{
    public class InMemoryPessimisticLockerFactory: IPessimisticLockerFactory
    {
        readonly InMemoryPessimisticLocker _instance = new InMemoryPessimisticLocker();

        public IPessimisticLocker Create()
        {
            return _instance;
        }
    }
}