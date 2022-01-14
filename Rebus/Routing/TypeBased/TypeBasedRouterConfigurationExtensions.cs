using System;
using System.Collections.Generic;
using Rebus.Config;
using Rebus.Logging;

namespace Rebus.Routing.TypeBased;

/// <summary>
/// Configuration extensions for configuring type-based routing (i.e. routing where each message type has one, single unambiguous
/// owning endpoint)
/// </summary>
public static class TypeBasedRouterConfigurationExtensions
{
    /// <summary>
    /// Configures Rebus to use type-based routing
    /// </summary>
    public static TypeBasedRouterConfigurationBuilder TypeBased(this StandardConfigurer<IRouter> configurer)
    {
        var builder = new TypeBasedRouterConfigurationBuilder();
        configurer.Register(c => builder.Build(c.Get<IRebusLoggerFactory>()));
        return builder;
    }

    /// <summary>
    /// Type-based routing configuration builder that can be called fluently to map message types to their owning endpoints
    /// </summary>
    public class TypeBasedRouterConfigurationBuilder
    {
        /// <summary>
        /// We use this way of storing configuration actions in order to preserve the order
        /// </summary>
        readonly List<Action<TypeBasedRouter>> _configurationActions = new List<Action<TypeBasedRouter>>();

        internal TypeBasedRouterConfigurationBuilder()
        {
        }

        /// <summary>
        /// Maps <paramref name="destinationAddress"/> as the owner of the <typeparamref name="TMessage"/> message type
        /// </summary>
        public TypeBasedRouterConfigurationBuilder Map<TMessage>(string destinationAddress)
        {
            _configurationActions.Add(r => r.Map<TMessage>(destinationAddress));
            return this;
        }

        /// <summary>
        /// Maps <paramref name="destinationAddress"/> as the owner of the <paramref name="messageType"/> message type
        /// </summary>
        public TypeBasedRouterConfigurationBuilder Map(Type messageType, string destinationAddress)
        {
            _configurationActions.Add(r => r.Map(messageType, destinationAddress));
            return this;
        }

        /// <summary>
        /// Maps <paramref name="destinationAddress"/> as the owner of all message types found in the same assembly as <typeparamref name="TMessage"/>
        /// </summary>
        public TypeBasedRouterConfigurationBuilder MapAssemblyOf<TMessage>(string destinationAddress)
        {
            _configurationActions.Add(r => r.MapAssemblyOf<TMessage>(destinationAddress));
            return this;
        }

        /// <summary>
        /// Maps <paramref name="destinationAddress"/> as the owner of all message types found in the same assembly as <paramref name="messageType"/>
        /// </summary>
        public TypeBasedRouterConfigurationBuilder MapAssemblyOf(Type messageType, string destinationAddress)
        {
            _configurationActions.Add(r => r.MapAssemblyOf(messageType, destinationAddress));
            return this;
        }

        /// <summary>
        /// Maps <paramref name="destinationAddress"/> as the owner of all message types found in the same assembly as <typeparamref name="TDerivedFrom"/>
        /// and derived from <typeparamref name="TDerivedFrom"/>
        /// </summary>
        public TypeBasedRouterConfigurationBuilder MapAssemblyDerivedFrom<TDerivedFrom>(string destinationAddress)
        {
            _configurationActions.Add(r => r.MapAssemblyDerivedFrom<TDerivedFrom>(destinationAddress));
            return this;
        }

        /// <summary>
        /// Maps <paramref name="destinationAddress"/> as the owner of all message types found in the same assembly as <typeparamref name="TMessage"/> under
        /// the namespace that type lives under. So all types within the same namespace will get mapped to that destination address, but not types under
        /// other namespaces. This allows you to separate messages for specific queues by namespace and register them all in one go.
        /// </summary>
        public TypeBasedRouterConfigurationBuilder MapAssemblyNamespaceOf<TMessage>(string destinationAddress)
        {
            _configurationActions.Add(r => r.MapAssemblyNamespaceOf<TMessage>(destinationAddress));
            return this;
        }

        /// <summary>
        /// Maps <paramref name="destinationAddress"/> as the owner of all message types found in the same assembly as <paramref name="messageType"/> under
        /// the namespace that type lives under. So all types within the same namespace will get mapped to that destination address, but not types under
        /// other namespaces. This allows you to separate messages for specific queues by namespace and register them all in one go.
        /// </summary>
        public TypeBasedRouterConfigurationBuilder MapAssemblyNamespaceOf(Type messageType, string destinationAddress)
        {
            _configurationActions.Add(r => r.MapAssemblyNamespaceOf(messageType, destinationAddress));
            return this;
        }

        /// <summary>
        /// Maps <paramref name="destinationAddress"/> as the owner of all message types found in the same assembly as <typeparamref name="TMessage"/> under
        /// the namespace that type lives under. So all types within the same namespace will get mapped to that destination address, but not types under
        /// other namespaces. This allows you to separate messages for specific queues by namespace and register them all in one go.
        /// </summary>
        public TypeBasedRouterConfigurationBuilder MapAssemblyNamespaceOfDerivedFrom<TMessage, TDerivedFrom>(string destinationAddress)
        {
            _configurationActions.Add(r => r.MapAssemblyNamespaceOfDerivedFrom<TMessage, TDerivedFrom>(destinationAddress));
            return this;
        }

        /// <summary>
        /// Maps <paramref name="destinationAddress"/> as the owner of all message types found in the same assembly as <paramref name="messageType"/> under
        /// the namespace that type lives under. So all types within the same namespace will get mapped to that destination address, but not types under
        /// other namespaces. This allows you to separate messages for specific queues by namespace and register them all in one go.
        /// </summary>
        public TypeBasedRouterConfigurationBuilder MapAssemblyNamespaceOfDerivedFrom(Type messageType, Type derivedFrom, string destinationAddress)
        {
            _configurationActions.Add(r => r.MapAssemblyNamespaceOfDerivedFrom(messageType, derivedFrom, destinationAddress));
            return this;
        }

        /// <summary>
        /// Maps <paramref name="destinationAddress"/> as a fallback destination to use when none of the configured mappings match
        /// </summary>
        public TypeBasedRouterConfigurationBuilder MapFallback(string destinationAddress)
        {
            _configurationActions.Add(r => r.MapFallback(destinationAddress));
            return this;
        }

        internal TypeBasedRouter Build(IRebusLoggerFactory rebusLoggerFactory)
        {
            var router = new TypeBasedRouter(rebusLoggerFactory);

            foreach (var action in _configurationActions)
            {
                action(router);
            }

            return router;
        }
    }
}