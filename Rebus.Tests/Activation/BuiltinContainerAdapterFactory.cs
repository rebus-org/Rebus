using System;
using Rebus.Activation;
using Rebus.Bus;
using Rebus.Handlers;
using Rebus.Tests.Contracts.Activation;

namespace Rebus.Tests.Activation
{
    public class BuiltinContainerAdapterFactory : IContainerAdapterFactory
    {
        readonly BuiltinHandlerActivator _builtinHandlerActivator = new BuiltinHandlerActivator();

        public IHandlerActivator GetActivator()
        {
            return _builtinHandlerActivator;
        }

        public void RegisterHandlerType<THandler>() where THandler : class, IHandleMessages
        {
            _builtinHandlerActivator.Register(() => (THandler) Activator.CreateInstance(typeof (THandler)));
        }

        public void CleanUp()
        {
            _builtinHandlerActivator.Dispose();
        }

        public IBus GetBus()
        {
            return _builtinHandlerActivator.Bus;
        }
    }
}