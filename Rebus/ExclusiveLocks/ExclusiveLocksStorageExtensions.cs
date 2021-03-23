using System;
using Rebus.Bus.Advanced;
using Rebus.Config;

namespace Rebus.ExclusiveLocks
{
    /// <summary>
    /// Configuration extensions for exclusive locks storage
    /// </summary>
    public static class ExclusiveLocksStorageExtensions
    {
        /// <summary>
        /// Configures Rebus to use a custom locker class for managing exclusive locks
        /// </summary>
        public static void UseCustomerLocker(this StandardConfigurer<IExclusiveAccessLock> configurer, IExclusiveAccessLock locker)
        {
            if (configurer == null) throw new ArgumentNullException(nameof(configurer));
            configurer.Register(c => locker);
        }
    }
}