using Rebus.Config;

namespace Rebus.Routing.TopicBased
{
    /// <summary>
    /// Router configuration extensions that help with setting up a topic-based routing
    /// </summary>
    public static class TopicBasedRouterConfigurationExtensions
    {
        /// <summary>
        /// Selectes topic-based routing
        /// </summary>
        public static TopicBasedRouterConfigurationBuilder TopicBased(this StandardConfigurer<IRouter> configurer)
        {
            var router = new TopicBasedRouter();
            var builder = new TopicBasedRouterConfigurationBuilder(router);
            configurer.Register(c => router);
            return builder;
        }

        /// <summary>
        /// Builder that can help with mapping topics to addresses
        /// </summary>
        public class TopicBasedRouterConfigurationBuilder
        {
            readonly TopicBasedRouter _router;

            /// <summary>
            /// Constructs the builder
            /// </summary>
            public TopicBasedRouterConfigurationBuilder(TopicBasedRouter router)
            {
                _router = router;
            }

            /// <summary>
            /// Maps the specified topic to the specified address
            /// </summary>
            public TopicBasedRouterConfigurationBuilder Map(string topic, string ownerAddress)
            {
                _router.Map(topic, ownerAddress);
                return this;
            }
        }
    }
}