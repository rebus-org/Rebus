using Rebus.Sagas.Locking;
using Rebus.Tests.Contracts.Locks;

namespace Rebus.Tests.Persistence.SqlServer
{
    public class SqlServerPessimisticLockerFactory : IPessimisticLockerFactory
    {
        public IPessimisticLocker Create()
        {
            throw new System.NotImplementedException();
        }
    }
}