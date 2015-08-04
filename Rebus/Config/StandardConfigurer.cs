using System;
using Rebus.Injection;

namespace Rebus.Config
{
    /// <summary>
    /// Configurer that can have extension methods attached to it for helping with registering an implementation or a decorator
    /// for the <typeparamref name="TService"/> service.
    /// </summary>
    public class StandardConfigurer<TService>
    {
        readonly Injectionist _injectionist;

        internal StandardConfigurer(Injectionist injectionist)
        {
            _injectionist = injectionist;
        }

        /// <summary>
        /// Registers the given factory function as a resolve of the given <typeparamref name="TService"/> service
        /// </summary>
        public void Register(Func<IResolutionContext, TService> factoryMethod)
        {
            _injectionist.Register(factoryMethod);
        }

        /// <summary>
        /// Registers the given factory function as a resolve of the given <typeparamref name="TService"/> service
        /// </summary>
        public void Decorate(Func<IResolutionContext, TService> factoryMethod)
        {
            _injectionist.Decorate(factoryMethod);
        }
    }
}