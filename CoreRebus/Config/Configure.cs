using System;
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
            if (handlerActivator == null) throw new ArgumentNullException(nameof(handlerActivator), @"Please remember to pass a handler activator to the .With(..) method.

The handler activator is responsible for looking up handlers for incoming messages, which makes for a pretty good place to use an adapter for an IoC container (because then your handlers can have dependencies injected).

If you are interested in a lightweight approach that does not depend on an IoC container, you can pass an instance of BuiltinHandlerActivator which can then be used to register handlers - either inline like this:

    activator.Handle<MyMessage>(async (bus, context, message) => {
        // handle message here
    });

or by registering a factory function like this:

    activator.Register(() => new MyHandler());
");

            return new RebusConfigurer(handlerActivator);
        }
    }
}