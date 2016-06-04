using Rebus.Sagas;

namespace Rebus.Tests.Sagas.Locking
{
    public static class InMemoryPessimisticSagaLockingConfigurationExtensions
    {
        public static void UseInMemoryLock(this PessimisticSagaLockingConfigurer configurer, InMemorySagaLocks inMemorySagaLocks)
        {
            configurer.Decorate<ISagaStorage>(c => new InMemoryPessimisticSagaLocker(c.Get<ISagaStorage>(), inMemorySagaLocks));
        }
    }
}