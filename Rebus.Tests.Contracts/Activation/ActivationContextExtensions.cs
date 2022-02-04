using System;
using Rebus.Activation;
using Rebus.Bus;
using Rebus.Config;

namespace Rebus.Tests.Contracts.Activation;

public static class ActivationContextExtensions
{
    public static IBus CreateBus(this IActivationContext context, Action<IHandlerRegistry> handlerConfig, Func<RebusConfigurer, RebusConfigurer> configureBus)
    {
        IActivatedContainer container;
        return context.CreateBus(handlerConfig, configureBus, out container);
    }

    public static IBus CreateBus(this IActivationContext context, Func<RebusConfigurer, RebusConfigurer> configureBus, out IActivatedContainer container)
    {
        return context.CreateBus(registry => {}, configureBus, out container);
    }

    public static IBus CreateBus(this IActivationContext context, Func<RebusConfigurer, RebusConfigurer> configureBus)
    {
        IActivatedContainer container;
        return context.CreateBus(registry => {}, configureBus, out container);
    }

    public static IHandlerActivator CreateActivator(this IActivationContext context)
    {
        IActivatedContainer container;
        return context.CreateActivator(handlers => {}, out container);
    }

    public static IHandlerActivator CreateActivator(this IActivationContext context, out IActivatedContainer container)
    {
        return context.CreateActivator(handlers => { }, out container);
    }

    public static IHandlerActivator CreateActivator(this IActivationContext context, Action<IHandlerRegistry> handlerConfig)
    {
        IActivatedContainer container;
        return context.CreateActivator(handlerConfig, out container);
    }
}