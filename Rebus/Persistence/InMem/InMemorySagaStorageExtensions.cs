using System;
using Rebus.Config;
using Rebus.Sagas;

namespace Rebus.Persistence.InMem;

/// <summary>
/// Configuration extensions for in-mem saga storage
/// </summary>
public static class InMemorySagaStorageExtensions
{
    /// <summary>
    /// Configures Rebus to store sagas in memory. Please note that while this method can be used for production purposes
    /// (if you need a saga storage that is pretty fast), it is probably better to use a persistent storage (like SQL Server
    /// or another database), because the state of all sagas will be lost in case the endpoint is restarted.
    /// </summary>
    public static void StoreInMemory(this StandardConfigurer<ISagaStorage> configurer)
    {
        if (configurer == null) throw new ArgumentNullException(nameof(configurer));
        configurer.Register(c => new InMemorySagaStorage());
    }
}