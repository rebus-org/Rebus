using Rebus2.Config;

namespace Rebus2.Routing.TopicBased
{
    public static class TopicBasedRouterConfigurationExtensions
    {
        public static TopicBasedRouterConfigurationBuilder TopicBased(this StandardConfigurer<IRouter> configurer)
        {
            var router = new TopicBasedRouter();
            var builder = new TopicBasedRouterConfigurationBuilder(router);
            configurer.Register(c => router);
            return builder;
        }

        public class TopicBasedRouterConfigurationBuilder
        {
            readonly TopicBasedRouter _router;

            public TopicBasedRouterConfigurationBuilder(TopicBasedRouter router)
            {
                _router = router;
            }

            public TopicBasedRouterConfigurationBuilder Map(string topic, string ownerAddress)
            {
                _router.Map(topic, ownerAddress);
                return this;
            }
        }
    }
}