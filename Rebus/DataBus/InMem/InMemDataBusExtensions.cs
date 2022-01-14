using System;
using Rebus.Config;
using Rebus.Time;

namespace Rebus.DataBus.InMem;

/// <summary>
/// Configuration extensions for the in-mem data bus
/// </summary>
public static class InMemDataBusExtensions
{
    /// <summary>
    /// Configures the data bus to store data in memory. This is probably only useful for test scenarios, as the
    /// passed-in <paramref name="inMemDataStore"/> needs to be shared among endpoints on the data bus.
    /// </summary>
    public static void StoreInMemory(this StandardConfigurer<IDataBusStorage> configurer, InMemDataStore inMemDataStore)
    {
        if (configurer == null) throw new ArgumentNullException(nameof(configurer));
        if (inMemDataStore == null) throw new ArgumentNullException(nameof(inMemDataStore));

        configurer.OtherService<InMemDataBusStorage>()
            .Register(c => new InMemDataBusStorage(inMemDataStore, c.Get<IRebusTime>()));

        configurer.Register(c => c.Get<InMemDataBusStorage>());

        configurer.OtherService<IDataBusStorageManagement>().Register(c => c.Get<InMemDataBusStorage>());
    }
}