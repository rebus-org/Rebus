using System;
using System.Linq;
using Rebus.Activation;
using Rebus.Bus;
using Rebus.Handlers;
using Rebus.Tests.Contracts.Activation;
using StructureMap;

namespace Rebus.StructureMap.Tests
{
    public class StructureMapContainerAdapterFactory : IContainerAdapterFactory
    {
        readonly IContainer _container = new Container();

        public IHandlerActivator GetActivator()
        {
            return new StructureMapContainerAdapter(_container);
        }

        public void RegisterHandlerType<THandler>() where THandler : class, IHandleMessages
        {
            _container.Configure(c =>
            {
                foreach (var handler in GetHandlerInterfaces(typeof (THandler)))
                {
                    c.For(handler).Use(typeof (THandler)).Transient();
                }
            });
        }

        public void CleanUp()
        {
            _container.Dispose();
        }

        public IBus GetBus()
        {
            return _container.GetInstance<IBus>();
        }

        Type[] GetHandlerInterfaces(Type type)
        {
            return type.GetInterfaces()
                .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IHandleMessages<>))
                .ToArray();
        }
    }
}