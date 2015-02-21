using System;
using Rebus2.Injection;

namespace Rebus2.Config
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

        public void Register<TService>(Func<IResolutionContext, TService> resolverMethod)
        {
            _injectionist.Register(resolverMethod);
        }
    }
}