using Rebus.Activation;

namespace Rebus.Config
{
    /// <summary>
    /// Configuration entry point - call the static <see cref="With"/> method with the chosen implementation of <see cref="IHandlerActivator"/> 
    /// (e.g. <see cref="BuiltinHandlerActivator"/>, or one that is backed by your favorite IoC container) in order to start configuring a
    /// Rebus instance
    /// </summary>
    public class Configure
    {
        /// <summary>
        /// Call this method with the chosen implementation of <see cref="IHandlerActivator"/> (e.g. <see cref="BuiltinHandlerActivator"/>, or one 
        /// that is backed by your favorite IoC container) in order to start configuring a
        /// Rebus instance
        /// </summary>
        public static RebusConfigurer With(IHandlerActivator handlerActivator)
        {
            return new RebusConfigurer(handlerActivator);
        }
    }
}