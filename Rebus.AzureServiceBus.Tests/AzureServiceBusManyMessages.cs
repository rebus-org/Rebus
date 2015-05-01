using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NUnit.Framework;
using Rebus.Activation;
using Rebus.Bus;
using Rebus.Config;
using Rebus.Tests;
using Rebus.Tests.Integration.ManyMessages;

namespace Rebus.AzureServiceBus.Tests
{
    [TestFixture]
    public class AzureServiceBusManyMessages : TestManyMessages<AzureServiceBusBusFactory>
    {
    }

    public class AzureServiceBusBusFactory : IBusFactory
    {
        readonly List<IDisposable> _stuffToDispose = new List<IDisposable>();

        public IBus GetBus<TMessage>(string inputQueueAddress, Func<TMessage, Task> handler)
        {
            var builtinHandlerActivator = new BuiltinHandlerActivator();

            builtinHandlerActivator.Handle(handler);

            var queueName = TestConfig.QueueName(inputQueueAddress);

            PurgeQueue(queueName);

            var bus = Configure.With(builtinHandlerActivator)
                .Transport(t => t.UseAzureServiceBus(AzureServiceBusTransportFactory.ConnectionString, queueName))
                .Options(o =>
                {
                    o.SetNumberOfWorkers(10);
                    o.SetMaxParallelism(10);
                })
                .Start();

            _stuffToDispose.Add(bus);

            return bus;
        }

        static void PurgeQueue(string queueName)
        {
            new AzureServiceBusTransport(AzureServiceBusTransportFactory.ConnectionString, queueName)
                .PurgeInputQueue();
        }

        public void Cleanup()
        {
            _stuffToDispose.ForEach(d => d.Dispose());
            _stuffToDispose.Clear();
        }
    }
}