using Rebus.Activation;

namespace Rebus.Config
{
    public class Configure
    {
        public static RebusConfigurer With(IHandlerActivator handlerActivator)
        {
            return new RebusConfigurer(handlerActivator);
        }
    }
}