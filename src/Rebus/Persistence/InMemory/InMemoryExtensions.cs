using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Rebus.Persistence.InMemory
{
    public static class InMemoryExtensions
    {
        public static void StoreInMemory(this Rebus.Configuration.Configurers.SagaConfigurer configurer)
        {
            configurer.Use(new InMemorySagaPersister());
        }

        public static void StoreInMemory(this Rebus.Configuration.Configurers.SubscriptionsConfigurer configurer)
        {
            configurer.Use(new InMemorySubscriptionStorage());
        }
    }
}
