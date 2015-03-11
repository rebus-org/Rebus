using System;
using System.Collections.Generic;
using System.Linq;

using Rebus;
using Rebus.Messages;
using Rebus.Configuration;

namespace Rebus.IdempotentSagas
{
    public static class IdempotentSagaExtensions
    {
        public static RebusSagasConfigurer WithIdempotentSagas(this RebusSagasConfigurer configurer)
        {
            var manager = new IdempotentSagasManager(configurer.Backbone);
            return configurer;
        }
    }
}
