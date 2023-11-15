using System;
using Rebus.Activation;

namespace Rebus.Config;

/// <summary>
/// Configuration entry point - call the static <see cref="With"/> method with the chosen implementation of <see cref="IHandlerActivator"/> (e.g. <see cref="BuiltinHandlerActivator"/>, or one that is backed by
/// your favorite IoC container) in order to start configuring a Rebus instance. If your app is hosted in Microsoft's generic host, please consider using Rebus.ServiceProvider and use the
/// <code>
/// services.AddRebus(
///     configure => configure
///         .(...)
/// );
/// </code>
/// way of configuring Rebus instead.
/// </summary>
public class Configure
{
    /// <summary>
    /// Call this method with the chosen implementation of <see cref="IHandlerActivator"/> (e.g. <see cref="BuiltinHandlerActivator"/>, or one 
    /// that is backed by your favorite IoC container) in order to start configuring a Rebus instance.
    /// If your app is hosted in Microsoft's generic host, please consider using Rebus.ServiceProvider and use the
    /// <code>
    /// services.AddRebus(
    ///     configure => configure
    ///         .(...)
    /// );
    /// </code>
    /// way of configuring Rebus instead.
    /// </summary>
    public static RebusConfigurer With(IHandlerActivator handlerActivator)
    {
        if (handlerActivator == null) throw new ArgumentNullException(nameof(handlerActivator), @"Please remember to pass a handler activator to the .With(..) method.

The handler activator is responsible for looking up handlers for incoming messages, which makes for a pretty good place to use an adapter for an IoC container (because then your handlers can have dependencies injected).
For more details on how to integrate with IoC containers, please look for the respective IoC container integrations on GitHub (e.g. Rebus.ServiceProvider, Rebus.Autofac, etc.).

If you are interested in a lightweight approach that does not depend on an IoC container, you can pass an instance of BuiltinHandlerActivator which can then be used to register handlers - either inline like this:

    activator.Handle<MyMessage>(async (bus, context, message) => {
        // handle message here
    });

or by registering a factory function like this:

    activator.Register(() => new MyHandler());

If your Rebus instance is not going to handle any messages, i.e. you want to configure a one-way client, you may configure Rebus by going

    var bus = Configure.OneWayClient()
                .Transport(t => t.Use(...)AsOneWayClient(...))
                .Start();

    // start sending/publishing!
");

        return new RebusConfigurer(handlerActivator);
    }

    /// <summary>
    /// Call this method when you're using one of the Transport(t => t.***AsOneWayClient()) transports and you don't have any <see cref="IHandlerActivator"/> to register.
    /// </summary>
    public static RebusConfigurer OneWayClient() =>
        With(new EmptyActivator())
            .Options(o => o.ValidateOneWayClient());
}