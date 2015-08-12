using System;
using Microsoft.Practices.Unity;
using Rebus.Configuration;
using Rebus.Unity;

namespace Rebus.Tests.Contracts.ContainerAdapters.Factories
{
    public class UnityContainerAdapterFactory : IContainerAdapterFactory
    {
        IUnityContainer container;

        public IContainerAdapter Create()
        {
            container = new UnityContainer();
            return new UnityContainerAdapter(container);
        }

        public void DisposeInnerContainer()
        {
            container.Dispose();
        }

        public void StartUnitOfWork()
        {
            
        }

        public void EndUnitOfWork()
        {
            
        }

        public void Register<TService, TImplementation>() where TService : class where TImplementation : TService
        {
            container.RegisterType(typeof (TService), typeof (TImplementation), Guid.NewGuid().ToString());
        }
    }
}