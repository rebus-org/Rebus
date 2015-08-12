using System;
using Rebus.Configuration;
using Rebus.SimpleInjector;
using SimpleInjector;

namespace Rebus.Tests.Contracts.ContainerAdapters.Factories
{
    public class SimpleInjectorContainerAdapterFactory
        : IContainerAdapterFactory
    {
        Container container;

        public IContainerAdapter Create()
        {
            container = new Container();
            return new SimpleInjectorAdapter(container);
        }

        public void DisposeInnerContainer()
        {
            if (HasComponent(typeof (IBus)))
            {
                var bus = container.GetInstance<IBus>();
                bus.Dispose();
            }
        }

        public void StartUnitOfWork() {}

        public void EndUnitOfWork() {}

        public void Register<TService, TImplementation>()
            where TService : class
            where TImplementation : TService
        {
            // container.Register(typeof (TService), typeof (TImplementation), Lifestyle.Transient);
            container.RegisterAll(typeof (TService), new[] {typeof (TImplementation)});
        }

        public bool HasComponent(Type componentType)
        {
            return container.GetRegistration(componentType, false) != null;
        }
    }
}