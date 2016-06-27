using Rebus.Config;
using Rebus.Routing;
using Rebus.Routing.TypeBased;

namespace Rebus.Recipes.Configuration
{
    public static class RoutingExtensions
    {
        public static void SendAllMessagesTo(this StandardConfigurer<IRouter> routeConfigurer, string queueName)
        {
            routeConfigurer.TypeBased().MapFallback(queueName);
        }
    }
}
