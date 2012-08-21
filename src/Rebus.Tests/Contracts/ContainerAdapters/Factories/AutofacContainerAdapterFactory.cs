using Autofac;
using Rebus.Autofac;
using Rebus.Configuration;

namespace Rebus.Tests.Contracts.ContainerAdapters.Factories
{
    public class AutofacContainerAdapterFactory : IContainerAdapterFactory
    {
        IContainer container;

        public IContainerAdapter Create()
        {
            container = new ContainerBuilder().Build();
            return new AutofacContainerAdapter(container);
        }

        public void DisposeInnerContainer()
        {
            container.Dispose();
        }
    }
}