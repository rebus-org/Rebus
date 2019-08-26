using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Rebus.Config;

namespace Rebus.Injection
{
    /// <summary>
    /// Dependency injectionist that can be used for configuring a system of injected service implementations, possibly with decorators,
    /// with caching of instances so that the same instance of each class is used throughout the tree. Should probably not be used for
    /// anything at runtime, is only meant to be used in configuration scenarios.
    /// </summary>
    public class Injectionist
    {
        /// <summary>
        /// Gets the injectionist instance backing the given <paramref name="configurer"/>
        /// </summary>
        public static Injectionist FromStandardConfigurer<TService>(StandardConfigurer<TService> configurer) => configurer.Injectionist;

        readonly Injectionist _parent;

        class Handler
        {
            public Handler()
            {
                Decorators = new List<Resolver>();
            }

            public Resolver PrimaryResolver { get; private set; }

            public List<Resolver> Decorators { get; }

            public void AddResolver(Resolver resolver)
            {
                if (!resolver.IsDecorator)
                {
                    AddPrimary(resolver);
                }
                else
                {
                    AddDecorator(resolver);
                }
            }

            void AddDecorator(Resolver resolver)
            {
                Decorators.Insert(0, resolver);
            }

            void AddPrimary(Resolver resolver)
            {
                PrimaryResolver = resolver;
            }
        }

        readonly Dictionary<Type, Handler> _resolvers = new Dictionary<Type, Handler>();

        /// <summary>
        /// Creates an empty injectionist container
        /// </summary>
        public Injectionist()
        {
        }

        /// <summary>
        /// Creates an injectionist container which will use the passed-in <paramref name="parent"/> container as a backup resolver
        /// </summary>
        public Injectionist(Injectionist parent)
        {
            _parent = parent;
        }

        /// <summary>
        /// Starts a new resolution context, resolving an instance of the given <typeparamref name="TService"/>
        /// </summary>
        public ResolutionResult<TService> Get<TService>()
        {
            var resolutionContext = new ResolutionContext(_resolvers, ResolveRequested, _parent);
            var instance = resolutionContext.Get<TService>();
            return new ResolutionResult<TService>(instance, resolutionContext.TrackedInstances);
        }

        /// <summary>
        /// Event that is raised when the resolution of a top-level instance is requested
        /// </summary>
        public event Action<Type> ResolveRequested;

        /// <summary>
        /// Registers a factory method that can provide an instance of <typeparamref name="TService"/>. Optionally,
        /// the supplied <paramref name="description"/> will be used to report more comprehensible errors in case of
        /// conflicting registrations.
        /// </summary>
        public void Register<TService>(Func<IResolutionContext, TService> resolverMethod, string description = null)
        {
            Register(resolverMethod, description: description, isDecorator: false);
        }

        /// <summary>
        /// Registers a decorator factory method that can provide an instance of <typeparamref name="TService"/> 
        /// (i.e. the resolver is expected to call <see cref="IResolutionContext.Get{TService}"/> where TService
        /// is <typeparamref name="TService"/>. Optionally, the supplied <paramref name="description"/> will be used 
        /// to report more comprehensible errors in case of conflicting registrations.
        /// </summary>
        public void Decorate<TService>(Func<IResolutionContext, TService> resolverMethod, string description = null)
        {
            Register(resolverMethod, description: description, isDecorator: true);
        }

        /// <summary>
        /// Returns whether there exists a registration for the specified <typeparamref name="TService"/>.
        /// </summary>
        public bool Has<TService>(bool primary = true)
        {
            return ResolverHaveRegistrationFor<TService>(primary, _resolvers);
        }

        static bool ResolverHaveRegistrationFor<TService>(bool primary, Dictionary<Type, Handler> resolvers)
        {
            var key = typeof(TService);

            if (!resolvers.ContainsKey(key)) return false;

            var handler = resolvers[key];

            if (handler.PrimaryResolver != null) return true;

            if (!primary && handler.Decorators.Any()) return true;

            return false;
        }

        void Register<TService>(Func<IResolutionContext, TService> resolverMethod, bool isDecorator, string description)
        {
            var handler = GetOrCreateHandler<TService>();
            var resolver = new Resolver<TService>(resolverMethod, description: description, isDecorator: isDecorator);

            if (!isDecorator)
            {
                if (handler.PrimaryResolver != null)
                {
                    var message = $"Attempted to register {resolver}, but a primary registration already exists: {handler.PrimaryResolver}";

                    throw new InvalidOperationException(message);
                }
            }

            handler.AddResolver(resolver);
        }

        Handler GetOrCreateHandler<TService>()
        {
            if (_resolvers.TryGetValue(typeof(TService), out var handler)) return handler;

            handler = new Handler();
            _resolvers[typeof(TService)] = handler;

            return handler;
        }

        abstract class Resolver
        {
            protected Resolver(bool isDecorator)
            {
                IsDecorator = isDecorator;
            }

            public bool IsDecorator { get; }
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
                var role = IsDecorator ? "decorator ->" : "primary ->";
                var type = typeof(TService);

                return !string.IsNullOrWhiteSpace(_description)
                    ? $"{role} {type} ({_description})"
                    : $"{role} {type}";
            }
        }

        class ResolutionContext : IResolutionContext
        {
            readonly Dictionary<Type, int> _decoratorDepth = new Dictionary<Type, int>();
            readonly Dictionary<Type, Handler> _resolvers;
            readonly Action<Type> _serviceTypeRequested;
            readonly Dictionary<Type, object> _instances = new Dictionary<Type, object>();
            readonly List<object> _resolvedInstances = new List<object>();
            readonly Injectionist _parent;

            public ResolutionContext(Dictionary<Type, Handler> resolvers, Action<Type> serviceTypeRequested, Injectionist parent)
            {
                _resolvers = resolvers;
                _serviceTypeRequested = serviceTypeRequested;
                _parent = parent;
            }

            public bool Has<TService>(bool primary = true)
            {
                return ResolverHaveRegistrationFor<TService>(primary, _resolvers);
            }

            public void Track(IEnumerable instances)
            {
                _resolvedInstances.AddRange(instances.Cast<object>());
            }

            public TService Get<TService>()
            {
                var serviceType = typeof(TService);

                if (_instances.TryGetValue(serviceType, out var existingInstance))
                {
                    return (TService)existingInstance;
                }

                if (!_resolvers.ContainsKey(serviceType))
                {
                    if (_parent != null)
                    {
                        try
                        {
                            var resolutionResult = _parent.Get<TService>();

                            _resolvedInstances.AddRange(resolutionResult.TrackedInstances.Cast<object>());

                            return resolutionResult.Instance;
                        }
                        catch (ResolutionException exception)
                        {
                            throw new ResolutionException(exception, $"Could not find resolver for {serviceType} (neither in this container, nor in the parent container)");
                        }
                    }

                    throw new ResolutionException($"Could not find resolver for {serviceType}");
                }

                if (!_decoratorDepth.ContainsKey(serviceType))
                {
                    _decoratorDepth[serviceType] = 0;
                    _serviceTypeRequested?.Invoke(serviceType);
                }

                var handlerForThisType = _resolvers[serviceType];
                var depth = _decoratorDepth[serviceType]++;

                try
                {
                    var resolver = handlerForThisType
                        .Decorators
                        .Cast<Resolver<TService>>()
                        .Skip(depth)
                        .FirstOrDefault()
                                   ?? (Resolver<TService>)handlerForThisType.PrimaryResolver;

                    var instance = resolver.InvokeResolver(this);

                    _instances[serviceType] = instance;

                    if (!_resolvedInstances.Contains(instance))
                    {
                        _resolvedInstances.Add(instance);
                    }

                    return instance;
                }
                catch (ResolutionException)
                {
                    throw; //< let this one through
                }
                catch (Exception exception)
                {
                    throw new ResolutionException(exception, $"Could not resolve {serviceType} with decorator depth {depth} - registrations: {string.Join("; ", handlerForThisType)}");
                }
                finally
                {
                    _decoratorDepth[serviceType]--;
                }
            }

            public IEnumerable TrackedInstances => _resolvedInstances.ToList();
        }
    }
}
