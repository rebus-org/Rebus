using System;
using Rebus.Injection;

namespace Rebus.Config
{
    public class StandardConfigurer<TService>
    {
        readonly Injectionist _injectionist;

        public StandardConfigurer(Injectionist injectionist)
        {
            _injectionist = injectionist;
        }

        public void Register(Func<IResolutionContext, TService> factoryMethod)
        {
            _injectionist.Register(factoryMethod);
        }
    }
}