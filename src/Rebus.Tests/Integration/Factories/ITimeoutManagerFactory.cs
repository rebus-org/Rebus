namespace Rebus.Tests.Integration.Factories
{
    public interface ITimeoutManagerFactory
    {
        void Initialize(IBusFactory busFactoryToUse);
        void CleanUp();
        IBus CreateBus(string inputQueue, IActivateHandlers handlerActivator);
        void StartAll();
    }
}