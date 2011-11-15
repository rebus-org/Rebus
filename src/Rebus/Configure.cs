using Rebus.Configuration.Configurers;

namespace Rebus
{
    public class Configure
    {
        public static RebusConfigurer With(IContainerAdapter containerAdapter)
        {
            return new RebusConfigurer(containerAdapter);
        }
    }
}