using Rebus.Activation;

namespace Rebus.Config
{
    public class Configure
    {
        public static RebusConfigurer With(BuiltinHandlerActivator handlerActivator)
        {
            return new RebusConfigurer(handlerActivator);
        }
    }
}