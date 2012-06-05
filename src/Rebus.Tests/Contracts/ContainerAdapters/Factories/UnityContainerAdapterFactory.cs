using Microsoft.Practices.Unity;
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
    }
}