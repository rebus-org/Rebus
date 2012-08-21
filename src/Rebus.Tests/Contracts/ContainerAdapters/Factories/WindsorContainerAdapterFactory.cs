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

        public void DisposeInnerContainer()
        {
            container.Dispose();
        }
    }
}