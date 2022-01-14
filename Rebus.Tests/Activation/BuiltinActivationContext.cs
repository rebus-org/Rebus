using System;
using Rebus.Activation;
using Rebus.Bus;
using Rebus.Config;
using Rebus.Handlers;
using Rebus.Tests.Contracts.Activation;

namespace Rebus.Tests.Activation;

public class BuiltinActivationContext : IActivationContext
{
    public IHandlerActivator CreateActivator(Action<IHandlerRegistry> configureHandlers, out IActivatedContainer container)
    {
        var activator = new BuiltinHandlerActivator();
        configureHandlers?.Invoke(new HandlerRegistry(activator));

        container = new ActivatedContainer(activator);

        return activator;
    }

    public IBus CreateBus(Action<IHandlerRegistry> configureHandlers, Func<RebusConfigurer, RebusConfigurer> configureBus, out IActivatedContainer container)
    {
        var activator = new BuiltinHandlerActivator();
        configureHandlers(new HandlerRegistry(activator));
            
        container = new ActivatedContainer(activator);

        return configureBus(Configure.With(activator)).Start();
    }

    class HandlerRegistry : IHandlerRegistry
    {
        readonly BuiltinHandlerActivator _activator;

        public HandlerRegistry(BuiltinHandlerActivator activator)
        {
            _activator = activator;
        }

        public IHandlerRegistry Register<THandler>() where THandler : class, IHandleMessages
        {
            _activator.Register(() => (THandler) Activator.CreateInstance(typeof (THandler)));
            return this;
        }
    }

    class ActivatedContainer : IActivatedContainer
    {
        readonly BuiltinHandlerActivator _activator;

        public ActivatedContainer(BuiltinHandlerActivator activator)
        {
            _activator = activator;
        }

        public IBus ResolveBus()
        {
            return _activator.Bus;
        }

        public void Dispose()
        {
            _activator.Dispose();
        }
    }
}