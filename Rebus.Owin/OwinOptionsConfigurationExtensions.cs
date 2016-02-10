using System;
using Owin;
using Rebus.Bus;
using Rebus.Config;
using Rebus.Logging;

namespace Rebus.Owin
{
    /// <summary>
    /// Configuration extensions for adding web endpoints to a Rebus endpoint
    /// </summary>
    public static class OwinOptionsConfigurationExtensions
    {
        /// <summary>
        /// Adds a web host for the given <paramref name="listenUrl"/> using the given <paramref name="startup"/> action
        /// </summary>
        public static void AddWebHost(this OptionsConfigurer configurer, string listenUrl, Action<IAppBuilder> startup)
        {
            if (!configurer.Has<RebusWebHost>())
            {
                configurer.Register(c =>
                {
                    var rebusLoggerFactory = c.Get<IRebusLoggerFactory>();

                    return new RebusWebHost(rebusLoggerFactory);
                });

                configurer.Decorate(c =>
                {
                    // make the Injectionist track the web host
                    c.Get<RebusWebHost>();

                    // it will be initialized when resolving the bus
                    return c.Get<IBus>();
                });
            }

            configurer.Decorate(c =>
            {
                var rebusWebHost = c.Get<RebusWebHost>();

                rebusWebHost.AddEndpoint(listenUrl, startup);

                return rebusWebHost;
            });
        }
    }
}
