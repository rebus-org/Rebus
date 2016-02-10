using System;
using Owin;
using Rebus.Bus;
using Rebus.Config;
using Rebus.Logging;

namespace Rebus.Owin
{
    public static class OwinOptionsConfigurationExtensions
    {
        public static void AddWebHost(this OptionsConfigurer configurer, string listenUrl, Action<IAppBuilder> startup)
        {
            configurer.Register(c => new RebusWebHost(c.Get<IRebusLoggerFactory>(), listenUrl, startup));

            configurer.Decorate(c =>
            {
                // make the Injectionist track the web host
                c.Get<RebusWebHost>();

                // it will be initialized when resolving the bus
                return c.Get<IBus>();
            });
        }
    }
}
