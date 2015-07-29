using Rebus.Configuration;
using Rebus.StructureMap;
using StructureMap;

namespace Rebus.Tests.Contracts.ContainerAdapters.Factories
{
    public class StructureMapContainerAdapterFactory : IContainerAdapterFactory
    {
        Container container;

        public IContainerAdapter Create()
        {
            container = new Container();
            return new StructureMapContainerAdapter(container);
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
            container.Configure(x => x.For<TService>().Add<TImplementation>());
        }
    }
}