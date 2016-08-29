using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Rebus.Activation;
using Rebus.Bus;
using Rebus.Extensions;
using Rebus.Handlers;
using Rebus.Tests.Contracts.Activation;

namespace Rebus.ServiceProvider.Tests
{
    public class NetCoreServiceCollectionContainerAdapterFactory : IContainerAdapterFactory
    {
        readonly IServiceCollection _serviceCollection = new ServiceCollection();

        public void CleanUp()
        {
            var serviceProvider = _serviceCollection.BuildServiceProvider();
            var bus = serviceProvider.GetService<IBus>();

            bus.Dispose();
        }

        public IHandlerActivator GetActivator()
        {
            return new NetCoreServiceCollectionContainerAdapter(_serviceCollection);
        }

        public IBus GetBus()
        {
            var container = _serviceCollection.BuildServiceProvider();

            return container.GetService<IBus>();
        }

        void IContainerAdapterFactory.RegisterHandlerType<THandler>()
        {
            GetHandlerInterfaces(typeof(THandler))
                .ForEach(i => _serviceCollection.AddTransient(i, typeof(THandler)));
        }

        static IEnumerable<Type> GetHandlerInterfaces(Type type)
        {
            return type.GetInterfaces()
                .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IHandleMessages<>))
                .ToArray();
        }
    }
}
