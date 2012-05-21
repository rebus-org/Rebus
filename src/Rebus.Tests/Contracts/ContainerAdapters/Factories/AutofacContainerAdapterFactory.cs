using Autofac;
using Rebus.Autofac;

namespace Rebus.Tests.Contracts.ContainerAdapters.Factories
{
    public class AutofacContainerAdapterFactory : IContainerAdapterFactory
    {
        public IContainerAdapter Create()
        {
            return new AutofacContainerAdapter(new ContainerBuilder().Build());
        }
    }
}