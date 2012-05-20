using Rebus.StructureMap;
using StructureMap;

namespace Rebus.Tests.Contracts.ContainerAdapters.Factories
{
    public class StructureMapContainerAdapterFactory : IContainerAdapterFactory
    {
        public IContainerAdapter Create()
        {
            return new StructureMapContainerAdapter(new Container());
        }
    }
}