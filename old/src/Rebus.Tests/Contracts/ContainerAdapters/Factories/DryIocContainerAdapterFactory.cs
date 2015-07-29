using DryIoc;
using Rebus.Configuration;
using Rebus.DryIoc;
using System.Linq;

namespace Rebus.Tests.Contracts.ContainerAdapters.Factories
{
    public class DryIocContainerAdapterFactory : IContainerAdapterFactory
    {
        private Container container;

        public IContainerAdapter Create()
        {
            container = new Container();
            return new DryIocContainerAdapter(container);
        }

        public void DisposeInnerContainer()
        {
            container.Dispose();
        }

        public void StartUnitOfWork()
        {
        }

        public void EndUnitOfWork()
        {
        }

        public void Register<TService, TImplementation>()
            where TService : class
            where TImplementation : TService
        {
            container.Register<TService, TImplementation>(withConstructor: type => type.GetConstructors().First());
        }
    }
}