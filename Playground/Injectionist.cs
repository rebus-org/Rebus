using System;
using System.Collections.Generic;
using System.Linq;

namespace Playground
{
    public class Injectionist
    {
        readonly Dictionary<Type, List<Delegate>> _resolvers = new Dictionary<Type, List<Delegate>>();

        public TService Get<TService>()
        {
            return new ResolutionContext(_resolvers).Get<TService>();
        }

        public void Register<TService>(Func<IResolutionContext, TService> resolver)
        {
            if (!_resolvers.ContainsKey(typeof (TService)))
                _resolvers.Add(typeof (TService), new List<Delegate>());

            _resolvers[typeof (TService)].Add(resolver);
        }

        class ResolutionContext : IResolutionContext
        {
            readonly Dictionary<Type, List<Delegate>> _resolvers;

            public ResolutionContext(Dictionary<Type, List<Delegate>> resolvers)
            {
                _resolvers = resolvers;
            }

            public TService Get<TService>()
            {
                if (!_resolvers.ContainsKey(typeof(TService)))
                {
                    throw new ApplicationException(string.Format("Could not find resolver for {0}", typeof(TService)));
                }

                var resolversForThisType = _resolvers[typeof(TService)];

                return resolversForThisType
                    .Cast<Func<IResolutionContext, TService>>()
                    .First()(this);
            }
        }
    }

    public interface IResolutionContext
    {
        TService Get<TService>();
    }
}
