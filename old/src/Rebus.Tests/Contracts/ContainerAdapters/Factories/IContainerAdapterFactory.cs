using Rebus.Configuration;

namespace Rebus.Tests.Contracts.ContainerAdapters.Factories
{
    public interface IContainerAdapterFactory
    {
        IContainerAdapter Create();
        void DisposeInnerContainer();
        void StartUnitOfWork();
        void EndUnitOfWork();

        void Register<TService, TImplementation>()
            where TImplementation : TService
            where TService : class;
    }
}