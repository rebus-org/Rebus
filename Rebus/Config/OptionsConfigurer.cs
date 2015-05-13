using System;
using Rebus.Injection;

namespace Rebus.Config
{
    /// <summary>
    /// Allows for configuring additional options
    /// </summary>
    public class OptionsConfigurer
    {
        readonly Options _options;
        readonly Injectionist _injectionist;

        internal OptionsConfigurer(Options options, Injectionist injectionist)
        {
            _options = options;
            _injectionist = injectionist;
        }

        /// <summary>
        /// Configures the number of workers to start competing over the input queue
        /// </summary>
        public OptionsConfigurer SetNumberOfWorkers(int numberOfWorkers)
        {
            _options.NumberOfWorkers = numberOfWorkers;
            return this;
        }

        /// <summary>
        /// Configures the degree of parallelism allowed within each worker
        /// </summary>
        public OptionsConfigurer SetMaxParallelism(int maxParallelism)
        {
            _options.MaxParallelism = maxParallelism;
            return this;
        }

        /// <summary>
        /// Registers the given factory function as a resolve of the given <see cref="TService"/> service
        /// </summary>
        public void Register<TService>(Func<IResolutionContext, TService> resolverMethod)
        {
            _injectionist.Register(resolverMethod);
        }

        /// <summary>
        /// Registers the given factory function as a resolve of the given <see cref="TService"/> service
        /// </summary>
        public void Decorate<TService>(Func<IResolutionContext, TService> factoryMethod)
        {
            _injectionist.Register(factoryMethod, isDecorator: true);
        }
    }
}