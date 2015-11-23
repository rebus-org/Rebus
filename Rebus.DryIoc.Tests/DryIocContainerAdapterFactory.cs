using DryIoc;
using Rebus.Activation;
using Rebus.Bus;
using Rebus.Handlers;
using Rebus.Tests.Contracts.Activation;

namespace Rebus.DryIoc.Tests
{
    public class DryIocContainerAdapterFactory : IContainerAdapterFactory
    {
        readonly IContainer _container = new Container(rules => rules
            .WithoutThrowOnRegisteringDisposableTransient()); // allows to register IDisposable transients

        public IHandlerActivator GetActivator()
        {
            return new DryIocContainerAdapter(_container);
        }

        public void RegisterHandlerType<THandler>() where THandler : class, IHandleMessages
        {
            _container.RegisterMany<THandler>(serviceTypeCondition: type => 
                type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IHandleMessages<>));
        }

        public void CleanUp()
        {
            _container.Dispose();
        }

        public IBus GetBus()
        {
            return _container.Resolve<IBus>();
        }
    }
}
