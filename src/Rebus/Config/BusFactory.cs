using System;
using Rebus.Bus;

namespace Rebus.Config
{
    public class BusFactory
    {
        public static IBus NewBus(Action<IBusConfigurer> configurer)
        {
            var configuration = new Configuration();
            configurer(configuration);
            return configuration.CreateTheBus();
        }
    }

    public interface IBusConfigurer
    {
        IBusConfigurer WithValue(string key, string value);
    }

    public class Configuration : IBusConfigurer
    {
        public IBus CreateTheBus()
        {
            return new RebusBus(null, null, null, null, null);
        }

        public IBusConfigurer WithValue(string key, string value)
        {
            return this;
        }
    }
}