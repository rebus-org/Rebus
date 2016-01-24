using System;
using Owin;
using Rebus.Bus;
using Rebus.Config;

namespace Rebus.Owin
{
    public static class OwinOptionsConfigurationExtensions
    {
        public static void AddWebHost(this OptionsConfigurer configurer, string listenUrl, Action<IAppBuilder> action)
        {
            configurer.Decorate(c =>
            {
                var bus = c.Get<IBus>();

                return bus;
            });
        }
    }

    class RebusWebHost
    {
        public RebusWebHost()
        {
            throw new NotImplementedException();
        }
    }
}
