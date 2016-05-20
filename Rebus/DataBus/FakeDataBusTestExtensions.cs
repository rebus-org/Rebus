using System;
using Rebus.Config;

namespace Rebus.DataBus
{
    static class FakeDataBusTestExtensions
    {
        public static void StoreInMemory(this StandardConfigurer<IDataBusStorage> configurer, InMemDataStore inMemDataStore)
        {
            if (inMemDataStore == null) throw new ArgumentNullException(nameof(inMemDataStore));

            configurer.Register(c => new InMemDataBusStorage(inMemDataStore));
        }
    }
}