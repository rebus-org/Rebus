using Rebus.Sagas.Locking;

namespace Rebus.Tests.Contracts.Locks
{
    public interface IPessimisticLockerFactory
    {
        IPessimisticLocker Create();
    }
}