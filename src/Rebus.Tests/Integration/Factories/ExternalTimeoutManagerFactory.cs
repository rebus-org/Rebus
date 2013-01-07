using Rebus.Persistence.InMemory;
using Rebus.Timeout;

namespace Rebus.Tests.Integration.Factories
{
    public class ExternalTimeoutManagerFactory : ITimeoutManagerFactory
    {
        TimeoutService timeoutService;
        IBusFactory busFactory;

        public void Initialize(IBusFactory busFactoryToUse)
        {
            timeoutService = new TimeoutService(new InMemoryTimeoutStorage());
            timeoutService.Start();
            busFactory = busFactoryToUse;
        }

        public void CleanUp()
        {
            timeoutService.Stop();
            busFactory.Cleanup();
        }

        public IBus CreateBus(string inputQueue, IActivateHandlers handlerActivator)
        {
            return busFactory.CreateBus(inputQueue, handlerActivator);
        }

        public void StartAll()
        {
            busFactory.StartAll();
        }
    }
}