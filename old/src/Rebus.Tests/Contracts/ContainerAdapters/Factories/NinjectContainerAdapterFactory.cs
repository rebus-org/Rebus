using Ninject;
using Rebus.Configuration;
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

        public void StartUnitOfWork()
        {
        }

        public void EndUnitOfWork()
        {
            
        }

        public void Register<TService, TImplementation>() where TService : class where TImplementation : TService
        {
            kernel.Bind<TService>().To<TImplementation>();
        }
    }
}