using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Practices.Unity;
using Rebus.Activation;
using Rebus.Handlers;
using Rebus.Tests.Contracts.Activation;

namespace Rebus.Unity.Tests
{
    public class UnityContainerAdapterFactory : IContainerAdapterFactory
    {
        readonly UnityContainer _unityContainer = new UnityContainer();

        public IHandlerActivator GetActivator()
        {
            return new UnityContainerAdapter(_unityContainer);
        }

        public void RegisterHandlerType<THandler>() where THandler : class, IHandleMessages
        {
            foreach (var handlerInterfaceType in GetHandlerInterfaces<THandler>())
            {
                var componentName = string.Format("{0}:{1}", typeof (THandler).FullName, handlerInterfaceType.FullName);

                _unityContainer.RegisterType(handlerInterfaceType, typeof(THandler), componentName, new TransientLifetimeManager(), new InjectionMember[0]);
            }
        }

        public void CleanUp()
        {
            _unityContainer.Dispose();
        }

        static IEnumerable<Type> GetHandlerInterfaces<THandler>() where THandler : class, IHandleMessages
        {
            return typeof (THandler).GetInterfaces().Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof (IHandleMessages<>));
        }
    }
}
