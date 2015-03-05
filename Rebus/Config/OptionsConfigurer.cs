using System;
using Rebus.Injection;

namespace Rebus.Config
{
    public class OptionsConfigurer
    {
        readonly Options _options;
        readonly Injectionist _injectionist;

        public OptionsConfigurer(Options options, Injectionist injectionist)
        {
            _options = options;
            _injectionist = injectionist;
        }

        public OptionsConfigurer SetNumberOfWorkers(int numberOfWorkers)
        {
            _options.NumberOfWorkers = numberOfWorkers;
            return this;
        }

        public OptionsConfigurer SetMaxParallelism(int maxParallelism)
        {
            _options.MaxParallelism = maxParallelism;
            return this;
        }

        public void Register<TService>(Func<IResolutionContext, TService> resolverMethod)
        {
            _injectionist.Register(resolverMethod);
        }
    }
}