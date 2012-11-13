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

        public void Register<TService, TImplementation>() where TService : class where TImplementation : TService
        {
            kernel.Bind<TService>().To<TImplementation>();
        }
    }

    public  class BuiltinContainerAdapterFactory : IContainerAdapterFactory
    {
        readonly BuiltinContainerAdapter builtinContainerAdapter = new BuiltinContainerAdapter();

        public IContainerAdapter Create()
        {
            return builtinContainerAdapter;
        }

        public void DisposeInnerContainer()
        {
            builtinContainerAdapter.Dispose();
        }

        public void Register<TService, TImplementation>() where TService : class where TImplementation : TService
        {
            builtinContainerAdapter.Register(typeof (TImplementation));
        }
    }
}