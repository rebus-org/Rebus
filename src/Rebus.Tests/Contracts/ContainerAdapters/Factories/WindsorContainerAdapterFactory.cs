using Castle.Windsor;
using Rebus.Castle.Windsor;

namespace Rebus.Tests.Contracts.ContainerAdapters.Factories
{
    public class WindsorContainerAdapterFactory : IContainerAdapterFactory
    {
        public IContainerAdapter Create()
        {
            return new WindsorContainerAdapter(new WindsorContainer());
        }
    }
}