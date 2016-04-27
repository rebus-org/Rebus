using System;
using Rebus.Config;

namespace Rebus.Routing.TopicBased
{
    /// <summary>
    /// Router configuration extensions that help with setting up a topic-based routing
    /// </summary>
    public static class TopicBasedRouterConfigurationExtensions
    {
        /// <summary>
        /// Selects topic-based routing. This simple type of routing can ONLY be used for PUB/SUB and it will go away soon
        /// </summary>
        [Obsolete("This way of configuring topic-based routing will go away soon - please use .Routing(r => r.Default().Map(...)) instead (the default routing is perfectly capable of doing topic-based routing too)")]
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