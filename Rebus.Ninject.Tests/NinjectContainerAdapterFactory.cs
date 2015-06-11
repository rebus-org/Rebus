using System;
using System.Collections.Generic;
using System.Linq;
using Ninject;
using Rebus.Activation;
using Rebus.Handlers;
using Rebus.Tests.Contracts.Activation;

namespace Rebus.Ninject.Tests
{
    public class NinjectContainerAdapterFactory : IContainerAdapterFactory
    {
        readonly StandardKernel _kernel = new StandardKernel();

        public IHandlerActivator GetActivator()
        {
            return new NinjectContainerAdapter(_kernel);
        }

        public void RegisterHandlerType<THandler>() where THandler : class, IHandleMessages
        {
            _kernel.Bind(GetHandlerInterfaces<THandler>().ToArray())
                .To<THandler>()
                .InTransientScope();
        }

        static IEnumerable<Type> GetHandlerInterfaces<THandler>() where THandler : class, IHandleMessages
        {
            return typeof(THandler).GetInterfaces().Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IHandleMessages<>));
        }

        public void CleanUp()
        {
            _kernel.Dispose();
        }
    }
}