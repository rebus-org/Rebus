using Castle.MicroKernel.Registration;
using Castle.Windsor;
using Rebus.Castle.Windsor;
using Rebus.Configuration;

namespace Rebus.Tests.Contracts.ContainerAdapters.Factories
{
    public class WindsorContainerAdapterFactory : IContainerAdapterFactory
    {
        WindsorContainer container;

        public IContainerAdapter Create()
        {
            container = new WindsorContainer();
            return new WindsorContainerAdapter(container);
        }

        public void StartUnitOfWork()
        {
        }

        public void EndUnitOfWork()
        {
            
        }

        public void Register<TService, TImplementation>()
            where TImplementation : TService
            where TService : class 
        {
            container.Register(Component.For<TService>().ImplementedBy<TImplementation>().LifestyleTransient());
        }

        public void DisposeInnerContainer()
        {
            container.Dispose();
        }
    }
}