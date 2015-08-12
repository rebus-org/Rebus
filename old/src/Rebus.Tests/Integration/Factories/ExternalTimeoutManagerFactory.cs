using System;
using Rebus.Persistence.InMemory;
using Rebus.Shared;
using Rebus.Timeout;

namespace Rebus.Tests.Integration.Factories
{
    public class ExternalTimeoutManagerFactory : ITimeoutManagerFactory
    {
        TimeoutService timeoutService;
        IBusFactory busFactory;

        public void Initialize(IBusFactory busFactoryToUse)
        {
            Console.WriteLine("Purging {0}, just to be sure", TimeoutService.DefaultInputQueueName);
            MsmqUtil.PurgeQueue(TimeoutService.DefaultInputQueueName);

            //var sqlServerTimeoutStorage = new SqlServerTimeoutStorage(SqlServerFixtureBase.ConnectionString, "rebus_timeouts").EnsureTableIsCreated();
            //var mongoDbTimeoutStorage = new MongoDbTimeoutStorage(MongoDbFixtureBase.ConnectionString, "timeouts");
            var inMemoryTimeoutStorage = new InMemoryTimeoutStorage();

            timeoutService = new TimeoutService(inMemoryTimeoutStorage);
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