using Rebus2.Config;

namespace Rebus2.Routing.TypeBased
{
    public static class SimpleTypeBasedRouterConfigurationExtensions
    {
        public static SimpleTypeBasedRouterConfigurationBuilder SimpleTypeBased(this StandardConfigurer<IRouter> configurer)
        {
            var router = new SimpleTypeBasedRouter();
            var builder = new SimpleTypeBasedRouterConfigurationBuilder(router);
            configurer.Register(c => router);
            return builder;
        }

        public class SimpleTypeBasedRouterConfigurationBuilder
        {
            readonly SimpleTypeBasedRouter _router;

            public SimpleTypeBasedRouterConfigurationBuilder(SimpleTypeBasedRouter router)
            {
                _router = router;
            }

            public SimpleTypeBasedRouterConfigurationBuilder Map<TMessage>(string destinationAddress)
            {
                _router.Map<TMessage>(destinationAddress);
                return this;
            }
        }
    }
}