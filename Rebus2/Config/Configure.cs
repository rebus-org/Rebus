using Rebus2.Activation;

namespace Rebus2.Config
{
    public class Configure
    {
        public static RebusConfigurer With(BuiltinHandlerActivator handlerActivator)
        {
            return new RebusConfigurer(handlerActivator);
        }
    }
}