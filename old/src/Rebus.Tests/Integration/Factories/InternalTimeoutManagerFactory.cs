using Rebus.Persistence.InMemory;

namespace Rebus.Tests.Integration.Factories
{
    public class InternalTimeoutManagerFactory : ITimeoutManagerFactory
    {
        IBusFactory busFactory;

        public void Initialize(IBusFactory busFactoryToUse)
        {
            busFactory = busFactoryToUse;
        }

        public void CleanUp()
        {
            busFactory.Cleanup();
        }

        public IBus CreateBus(string inputQueue, IActivateHandlers handlerActivator)
        {
            return busFactory.CreateBus(inputQueue, handlerActivator, new InMemoryTimeoutStorage());
        }

        public void StartAll()
        {
            busFactory.StartAll();
        }
    }
}