using Rebus.Config;
using Rebus.Sagas.Locking;

namespace Rebus.Persistence.InMem
{
    public static class InMemoryPessimisticSagaLockingConfigurationExtensions
    {
        public static void UseInMemoryLock(this StandardConfigurer<IPessimisticLock> configurer, InMemorySagaLocks inMemorySagaLocks)
        {
            configurer.Register(c => inMemorySagaLocks);
        }
    }
}