using System;
using Rebus.Activation;
using Rebus.Bus;
using Rebus.Config;
using Rebus.Handlers;

namespace Rebus.Tests.Contracts.Activation;

public interface IActivationContext
{
    IHandlerActivator CreateActivator(Action<IHandlerRegistry> configureHandlers, out IActivatedContainer container);

    IBus CreateBus(Action<IHandlerRegistry> configureHandlers, Func<RebusConfigurer, RebusConfigurer> configureBus, out IActivatedContainer container);
}

public interface IHandlerRegistry
{
    IHandlerRegistry Register<THandler>() where THandler : class, IHandleMessages;
}

public interface IActivatedContainer : IDisposable
{
    IBus ResolveBus();
}