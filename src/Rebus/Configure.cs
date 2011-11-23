using Rebus.Configuration.Configurers;

namespace Rebus
{
    public class Configure
    {
        public static RebusConfigurerWithLogging With(IContainerAdapter containerAdapter)
        {
            return new RebusConfigurerWithLogging(containerAdapter);
        }
    }
}