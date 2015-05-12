using Rebus.Config;

namespace Rebus.Routing.TypeBased
{
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
            var router = new TypeBasedRouter();
            var builder = new TypeBasedRouterConfigurationBuilder(router);
            configurer.Register(c => router);
            return builder;
        }

        /// <summary>
        /// Type-based routing configuration builder that can be called fluently to map message types to their owning endpoints
        /// </summary>
        public class TypeBasedRouterConfigurationBuilder
        {
            readonly TypeBasedRouter _router;

            internal TypeBasedRouterConfigurationBuilder(TypeBasedRouter router)
            {
                _router = router;
            }

            /// <summary>
            /// Maps <see cref="destinationAddress"/> as the owner of the <see cref="TMessage"/> message type
            /// </summary>
            public TypeBasedRouterConfigurationBuilder Map<TMessage>(string destinationAddress)
            {
                _router.Map<TMessage>(destinationAddress);
                return this;
            }

            /// <summary>
            /// Maps <see cref="destinationAddress"/> as the owner of all message types found in the same assembly as <see cref="TMessage"/>
            /// </summary>
            public TypeBasedRouterConfigurationBuilder MapAssemblyOf<TMessage>(string destinationAddress)
            {
                _router.MapAssemblyOf<TMessage>(destinationAddress);
                return this;
            }
        }
    }
}