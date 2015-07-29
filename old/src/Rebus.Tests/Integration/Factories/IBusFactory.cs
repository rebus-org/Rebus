using Rebus.Timeout;

namespace Rebus.Tests.Integration.Factories
{
    public interface IBusFactory
    {
        IBus CreateBus(string inputQueueName, IActivateHandlers handlerActivator);
        IBus CreateBus(string inputQueueName, IActivateHandlers handlerActivator, IStoreTimeouts storeTimeouts);
        void Cleanup();
        void StartAll();
    }
}