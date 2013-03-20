using Rebus.Configuration;

namespace Rebus.Tests.Contracts.ContainerAdapters.Factories
{
    public class BuiltinContainerAdapterFactory : IContainerAdapterFactory
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

        public void StartUnitOfWork()
        {
        }

        public void EndUnitOfWork()
        {
            
        }

        public void Register<TService, TImplementation>() where TService : class where TImplementation : TService
        {
            builtinContainerAdapter.Register(typeof (TImplementation));
        }
    }
}