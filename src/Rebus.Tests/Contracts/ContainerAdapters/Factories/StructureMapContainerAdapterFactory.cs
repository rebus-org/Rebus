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
    }
}