using System;
using System.Collections.Generic;
using System.Linq;

namespace Playground
{
    public class Injectionist
    {
        readonly Dictionary<Type, List<Resolver>> _resolvers = new Dictionary<Type, List<Resolver>>();

        public TService Get<TService>()
        {
            return new ResolutionContext(_resolvers).Get<TService>();
        }

        public void Register<TService>(Func<IResolutionContext, TService> resolverMethod, bool isDecorator = false)
        {
            if (!_resolvers.ContainsKey(typeof(TService)))
            {
                _resolvers.Add(typeof(TService), new List<Resolver>());
            }

            var resolverList = _resolvers[typeof(TService)];
            var resolver = new Resolver<TService>(resolverMethod, isDecorator: isDecorator);

            if (!resolver.IsDecorator)
            {
                resolverList.Add(resolver);
            }
            else
            {
                resolverList.Insert(0, resolver);
            }
        }

        abstract class Resolver { }

        class Resolver<TService> : Resolver
        {
            readonly Func<IResolutionContext, TService> _resolver;
            readonly bool _isDecorator;

            public Resolver(Func<IResolutionContext, TService> resolver, bool isDecorator)
            {
                _resolver = resolver;
                _isDecorator = isDecorator;
            }

            public bool IsDecorator
            {
                get { return _isDecorator; }
            }

            public TService InvokeResolver(IResolutionContext context)
            {
                return _resolver(context);
            }
        }

        class ResolutionContext : IResolutionContext
        {
            readonly Dictionary<Type, int> _decoratorDepth = new Dictionary<Type, int>();
            readonly Dictionary<Type, List<Resolver>> _resolvers;

            public ResolutionContext(Dictionary<Type, List<Resolver>> resolvers)
            {
                _resolvers = resolvers;
            }

            public TService Get<TService>()
            {
                if (!_resolvers.ContainsKey(typeof(TService)))
                {
                    throw new ResolutionException("Could not find resolver for {0}", typeof(TService));
                }

                if (!_decoratorDepth.ContainsKey(typeof (TService)))
                {
                    _decoratorDepth[typeof (TService)] = 0;
                }

                var resolversForThisType = _resolvers[typeof(TService)];

                try
                {
                    var depth = _decoratorDepth[typeof (TService)]++;

                    return resolversForThisType
                        .Cast<Resolver<TService>>()
                        .Skip(depth)
                        .First()
                        .InvokeResolver(this);
                }
                finally
                {
                    _decoratorDepth[typeof (TService)]--;
                }
            }
        }
    }

    public interface IResolutionContext
    {
        TService Get<TService>();
    }
}
