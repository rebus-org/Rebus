using System;
using System.Collections.Generic;
using System.Linq;
using Rebus.Activation;
using Rebus.Handlers;
using Rebus.Tests.Contracts.Activation;
using SimpleInjector;

namespace Rebus.SimpleInjector.Tests
{
    public class SimpleInjectorContainerAdapterFactory : IContainerAdapterFactory
    {
        readonly Container _container = new Container();

        public IHandlerActivator GetActivator()
        {
            return new SimpleInjectorContainerAdapter(_container);
        }

        public void RegisterHandlerType<THandler>() where THandler : class, IHandleMessages
        {
            _container.Register(typeof(THandler));

            _container.Register<THandler, THandler>();

            return;
            foreach (var handlerInterfaceType in GetHandlerInterfaces<THandler>())
            {
                var componentName = string.Format("{0}:{1}", typeof(THandler).FullName, handlerInterfaceType.FullName);

                //_container.RegisterType(handlerInterfaceType, typeof(THandler), componentName, new TransientLifetimeManager(), new InjectionMember[0]);
            }
        }

        public void CleanUp()
        {
        }

        static IEnumerable<Type> GetHandlerInterfaces<THandler>() where THandler : class, IHandleMessages
        {
            return typeof(THandler).GetInterfaces().Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IHandleMessages<>));
        }
    }
}
