using System;
using Rebus.Config;
using Rebus.Injection;

namespace Rebus.Tests.Sagas.Locking
{
    public class PessimisticSagaLockingConfigurer
    {
        readonly OptionsConfigurer _configurer;

        public PessimisticSagaLockingConfigurer(OptionsConfigurer configurer)
        {
            _configurer = configurer;
        }

        public void Decorate<TService>(Func<IResolutionContext, TService> resolverMethod, string description = null)
        {
            _configurer.Decorate(resolverMethod, description);
        }
    }
}