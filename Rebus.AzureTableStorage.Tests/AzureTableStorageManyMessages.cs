using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NUnit.Framework;
using Rebus.Activation;
using Rebus.Bus;
using Rebus.Config;
using Rebus.Tests;
using Rebus.Tests.Integration.ManyMessages;

namespace Rebus.AzureTableStorage.Tests
{
    [TestFixture]
    public class AzureTableStorageManyMessages : TestManyMessages<AzureTableStorageManyMessagesTransportFactory>
    {

    }


    public class AzureTableStorageManyMessagesTransportFactory : IBusFactory
    {
        private readonly List<IDisposable> _stuffToDispose = new List<IDisposable>();

        public IBus GetBus<TMessage>(string inputQueueAddress, Func<TMessage, Task> handler)
        {
            var builtinHandlerActivator = new BuiltinHandlerActivator();

            builtinHandlerActivator.Handle(handler);

            var queueName = TestConfig.QueueName(inputQueueAddress);

            PurgeQueue(queueName);

            var bus = Configure.With(builtinHandlerActivator)
                .Transport(
                    t =>
                        t.UseAzureTableStorage(AzureTableStorageTransportFactory.ConnectionString, queueName))
                .Options(o =>
                {
                    o.SetNumberOfWorkers(20);
                    o.SetMaxParallelism(4);
                })
                .Start();

            _stuffToDispose.Add(bus);

            return bus;
        }

        private static void PurgeQueue(string queueName)
        {
            new AzureTableStorageTransport(AzureTableStorageTransportFactory.ConnectionString, queueName).PurgeInputQueue();

        }

        public void Cleanup()
        {
            _stuffToDispose.ForEach(d => d.Dispose());
            _stuffToDispose.Clear();
        }
    }
}