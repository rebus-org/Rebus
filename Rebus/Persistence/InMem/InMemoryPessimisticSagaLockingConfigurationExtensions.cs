using System;
using Rebus.Config;
using Rebus.Sagas.Locking;

namespace Rebus.Persistence.InMem
{
    /// <summary>
    /// Configuration extensions for the in-mem pessimistic locking mechanism
    /// </summary>
    public static class InMemoryPessimisticSagaLockingConfigurationExtensions
    {
        /// <summary>
        /// Configures in-mem locking of saga data instances. If the <paramref name="inMemoryPessimisticLocker"/> parameter is set, it will be used
        /// for locking, thus enabling the sharing of it among multiple in-process endpoints. If it is not set, no locks are shared with other endpoints.
        /// </summary>
        public static void UseInMemoryLock(this StandardConfigurer<IPessimisticLocker> configurer, InMemoryPessimisticLocker inMemoryPessimisticLocker = null)
        {
            if (configurer == null) throw new ArgumentNullException(nameof(configurer));

            configurer.Register(c => inMemoryPessimisticLocker ?? new InMemoryPessimisticLocker());
        }
    }
}