using System;
using System.Collections.Generic;
using System.Linq;

namespace Rebus.Injection
{
    /// <summary>
    /// Dependency injectionist that can be used for configuring a system of injected service implementations, possibly with decorators,
    /// with caching of instances so that the same instance of each class is used throughout the tree. Should probably not be used for
    /// anything at runtime, is only meant to be used in configuration scenarios.
    /// </summary>
    class Injectionist
    {
        readonly Dictionary<Type, List<Resolver>> _resolvers = new Dictionary<Type, List<Resolver>>();

        /// <summary>
        /// Starts a new resolution context, resolving an instance of the given <typeparamref name="TService"/>
        /// </summary>
        public TService Get<TService>()
        {
            var resolutionContext = new ResolutionContext(_resolvers, serviceType => ResolveRequested(serviceType));

            return resolutionContext.Get<TService>();
        }

        /// <summary>
        /// Registers a factory method that can provide an instance of the primary implementation of <typeparamref name="TService"/>
        /// </summary>
        public void Register<TService>(Func<IResolutionContext, TService> resolverMethod, string description = null)
        {
            Register(resolverMethod, isDecorator: false, description: description);
        }

        /// <summary>
        /// Registers a factory method that can provide a decorator of <typeparamref name="TService"/>
        /// </summary>
        public void Decorate<TService>(Func<IResolutionContext, TService> resolverMethod, string description = null)
        {
            Register(resolverMethod, isDecorator: true, description: description);
        }

        void Register<TService>(Func<IResolutionContext, TService> resolverMethod, bool isDecorator, string description)
        {
            var key = typeof(TService);
            if (!_resolvers.ContainsKey(key))
            {
                _resolvers.Add(key, new List<Resolver>());
            }

            var resolverList = _resolvers[key];

            var resolver = new Resolver<TService>(resolverMethod, isDecorator: isDecorator, description: description);

            if (!isDecorator)
            {
                var existingPrimaryRegistration = resolverList.FirstOrDefault(r => !r.IsDecorator);

                if (existingPrimaryRegistration != null)
                {
                    throw new InvalidOperationException(string.Format("Attempted to register {0} as primary resolver of {1}, but a primary registration already exists: {2}",
                        resolver, typeof(TService), existingPrimaryRegistration));
                }
            }

            if (!resolver.IsDecorator)
            {
                resolverList.Add(resolver);
            }
            else
            {
                resolverList.Insert(0, resolver);
            }
        }

        /// <summary>
        /// Event that gets fired when (top level) resolve is called for a type
        /// </summary>
        public event Action<Type> ResolveRequested = delegate { };

        /// <summary>
        /// Returns whether there exists a primary registration for the specified <typeparamref name="TService"/>.
        /// </summary>
        public bool Has<TService>()
        {
            var key = typeof(TService);
            return _resolvers.ContainsKey(key)
                && _resolvers[key].Count(r => !r.IsDecorator) == 1;
        }

        abstract class Resolver
        {
            readonly bool _isDecorator;

            protected Resolver(bool isDecorator)
            {
                _isDecorator = isDecorator;
            }

            public bool IsDecorator
            {
                get { return _isDecorator; }
            }
        }

        class Resolver<TService> : Resolver
        {
            readonly Func<IResolutionContext, TService> _resolver;
            readonly string _description;

            public Resolver(Func<IResolutionContext, TService> resolver, bool isDecorator, string description)
                : base(isDecorator)
            {
                _resolver = resolver;
                _description = description;
            }

            public TService InvokeResolver(IResolutionContext context)
            {
                return _resolver(context);
            }

            public override string ToString()
            {
                var text = string.Format("({0} {1})",
                    IsDecorator ? "decorator ->" : "primary ->",
                    typeof(TService));

                if (!string.IsNullOrWhiteSpace(_description))
                {
                    text += string.Format(": {0}", _description);
                }

                return text;
            }
        }

        class ResolutionContext : IResolutionContext
        {
            readonly Dictionary<Type, int> _decoratorDepth = new Dictionary<Type, int>();
            readonly Dictionary<Type, List<Resolver>> _resolvers;
            readonly Action<Type> _serviceTypeRequested;
            readonly Dictionary<Type, object> _instances = new Dictionary<Type, object>();
            readonly List<object> _resolvedInstances = new List<object>();

            public ResolutionContext(Dictionary<Type, List<Resolver>> resolvers, Action<Type> serviceTypeRequested)
            {
                _resolvers = resolvers;
                _serviceTypeRequested = serviceTypeRequested;
            }

            public TService Get<TService>()
            {
                var serviceType = typeof(TService);

                if (_instances.ContainsKey(serviceType))
                {
                    return (TService)_instances[serviceType];
                }

                if (!_resolvers.ContainsKey(serviceType))
                {
                    throw new ResolutionException("Could not find resolver for {0}", serviceType);
                }

                if (!_decoratorDepth.ContainsKey(serviceType))
                {
                    _decoratorDepth[serviceType] = 0;
                    _serviceTypeRequested(serviceType);
                }

                var resolversForThisType = _resolvers[serviceType];
                var depth = _decoratorDepth[serviceType]++;

                try
                {
                    var instance = resolversForThisType
                        .Cast<Resolver<TService>>()
                        .Skip(depth)
                        .First()
                        .InvokeResolver(this);

                    _instances[serviceType] = instance;
                    _resolvedInstances.Add(instance);

                    return instance;
                }
                catch (ResolutionException)
                {
                    throw;
                }
                catch (Exception exception)
                {
                    throw new ResolutionException(exception, "Could not resolve {0} with decorator depth {1} - registrations: {2}",
                        serviceType, depth, string.Join(", ", resolversForThisType));
                }
                finally
                {
                    _decoratorDepth[serviceType]--;
                }
            }

            public IEnumerable<T> GetTrackedInstancesOf<T>()
            {
                return _resolvedInstances.OfType<T>().Distinct().ToList();
            }
        }
    }
}
