using System;
using Rebus.Config;
using Rebus.Exceptions;
using Rebus.Injection;

namespace Rebus.Sagas.Locking
{
    /// <summary>
    /// Configuration extensions for enabling pessimistic locking on saga data
    /// </summary>
    public static class PessimisticSagaLockingConfigurationExtensions
    {
        /// <summary>
        /// Default maximum time we accept to have to wait in order to acquire a lock
        /// </summary>
        public static readonly TimeSpan DefaultAcquireLockMaximumWaitTime = TimeSpan.FromSeconds(10);

        /// <summary>
        /// Enables pessimistic saga data locking whereby an appropriate (possibly distributed, depending on your needs) lock is obtained
        /// every time a saga is created/updated/deleted. When acquiring a lock, we might need to wait a while - the timeout for the
        /// wait period can be set by providing a value for <paramref name="acquireLockMaximumWaitTime"/>. If not explicitly set,
        /// <see cref="DefaultAcquireLockMaximumWaitTime"/> will be used.
        /// </summary>
        public static StandardConfigurer<IPessimisticLock> EnablePessimisticSagaLocking(this OptionsConfigurer configurer, TimeSpan? acquireLockMaximumWaitTime = null)
        {
            configurer.Decorate<ISagaStorage>(c =>
            {
                var acquireLockTimeout = acquireLockMaximumWaitTime ?? DefaultAcquireLockMaximumWaitTime;
                var pessimisticLock = GetPessimisticLock(c);
                var sagaStorage = c.Get<ISagaStorage>();
                return new LockingSagaStorageDecorator(sagaStorage, pessimisticLock, acquireLockTimeout);
            });

            return StandardConfigurer<IPessimisticLock>.GetConfigurerFrom(configurer);
        }

        static IPessimisticLock GetPessimisticLock(IResolutionContext c)
        {
            try
            {
                return c.Get<IPessimisticLock>();
            }
            catch (Exception exception)
            {
                throw new RebusApplicationException(exception, @"Could not get pessimistik lock - did you call 'EnablePessimisticSagaLocking' without choosing a way to lock the saga data instances?

When you enable pessimistic locking, you need to specify which kind of locking to use - it is done by making further calls after 'EnablePessimisticSagaLocking', e.g. like so:

Configure.With(..)
    .(...)
    .Options(o => o.EnablePessimisticSagaLocking().UseSqlServer(....))
    .(...)");
            }
        }
    }
}