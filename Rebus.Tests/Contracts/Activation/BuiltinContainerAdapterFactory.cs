using System;
using NUnit.Framework;
using Rebus.Activation;
using Rebus.Handlers;

namespace Rebus.Tests.Contracts.Activation
{
    [TestFixture]
    public class BuiltinHandlerActivatorContainerTests : ContainerTests<BuiltinContainerAdapterFactory> { }

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
    }
}