using Rebus2.Config;

namespace Rebus2.Routing.TypeBased
{
    public static class TypeBasedRouterConfigurationExtensions
    {
        public static TypeBasedRouterConfigurationBuilder TypeBased(this StandardConfigurer<IRouter> configurer)
        {
            var router = new TypeBasedRouter();
            var builder = new TypeBasedRouterConfigurationBuilder(router);
            configurer.Register(c => router);
            return builder;
        }

        public class TypeBasedRouterConfigurationBuilder
        {
            readonly TypeBasedRouter _router;

            public TypeBasedRouterConfigurationBuilder(TypeBasedRouter router)
            {
                _router = router;
            }

            public TypeBasedRouterConfigurationBuilder Map<TMessage>(string destinationAddress)
            {
                _router.Map<TMessage>(destinationAddress);
                return this;
            }
        }
    }
}