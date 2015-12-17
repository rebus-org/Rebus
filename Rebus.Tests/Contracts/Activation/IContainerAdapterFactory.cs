using Rebus.Activation;
using Rebus.Bus;
using Rebus.Handlers;

namespace Rebus.Tests.Contracts.Activation
{
    public interface IContainerAdapterFactory
    {
        IHandlerActivator GetActivator();

        void RegisterHandlerType<THandler>() where THandler : class, IHandleMessages;

        void CleanUp();

        IBus GetBus();
    }
}