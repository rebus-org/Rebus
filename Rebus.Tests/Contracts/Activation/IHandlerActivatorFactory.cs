using Rebus.Activation;
using Rebus.Handlers;

namespace Rebus.Tests.Contracts.Activation
{
    public interface IHandlerActivatorFactory
    {
        IHandlerActivator GetActivator();

        void RegisterHandlerType<THandler>() where THandler : class, IHandleMessages;

        void CleanUp();
    }
}