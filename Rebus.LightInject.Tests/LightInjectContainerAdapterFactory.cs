using System;
using System.Collections.Generic;
using System.Linq;
using LightInject;
using Rebus.Activation;
using Rebus.Bus;
using Rebus.Extensions;
using Rebus.Handlers;
using Rebus.Tests.Contracts.Activation;

namespace Rebus.LightInject.Tests
{
    public class LightInjectContainerAdapterFactory : IContainerAdapterFactory
    {
        readonly ServiceContainer _serviceContainer = new ServiceContainer();

        public IHandlerActivator GetActivator()
        {
            return  new LightInjectContainerAdapter(_serviceContainer);
        }

        public void RegisterHandlerType<THandler>() where THandler : class, IHandleMessages
        {
            foreach (var handlerInterfaceType in GetHandlerInterfaces<THandler>())
            {
                var componentName = string.Format("{0}:{1}", typeof(THandler).FullName, handlerInterfaceType.FullName);

                _serviceContainer.Register(handlerInterfaceType, typeof(THandler),componentName);
            }
        }

        public void CleanUp()
        {
            _serviceContainer.Dispose();
        }

        public IBus GetBus()
        {
            return _serviceContainer.GetInstance<IBus>();
        }

        static IEnumerable<Type> GetHandlerInterfaces<THandler>() where THandler : class, IHandleMessages
        {
            return typeof (THandler).GetInterfaces().Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof (IHandleMessages<>));
        }
    }
}
