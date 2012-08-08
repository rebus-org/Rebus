using Ninject;
using Rebus.Ninject;

namespace Rebus.Tests.Contracts.ContainerAdapters.Factories
{
    public class NinjectContainerAdapterFactory : IContainerAdapterFactory
    {
        IKernel kernel;

        public IContainerAdapter Create()
        {
            kernel = new StandardKernel();
            return new NinjectContainerAdapter(kernel);
        }

        public void DisposeInnerContainer()
        {
            kernel.Dispose();
        }
    }
}
