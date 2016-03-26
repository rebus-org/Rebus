using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ServiceBus;
using NUnit.Framework;
using Rebus.Activation;
using Rebus.AzureServiceBus.Config;
using Rebus.AzureServiceBus.Tests.Factories;
using Rebus.Bus;
using Rebus.Config;
using Rebus.Logging;
using Rebus.Tests;
using Rebus.Tests.Extensions;
using Rebus.Threading.TaskParallelLibrary;

namespace Rebus.AzureServiceBus.Tests
{



    [TestFixture, Category(TestCategory.Azure)]
    public class BasicAzureServiceBusBasicReceiveOnly : FixtureBase
    {
        
        static readonly string QueueName = TestConfig.QueueName("input");
       
        
          
       
        [Test]
        [TestCase(AzureServiceBusMode.Basic)]
        [TestCase(AzureServiceBusMode.Standard)]
        public async void ShouldBeAbleToRecieveEvenWhenNotCreatingQueue(AzureServiceBusMode mode)
        {
            var consoleLoggerFactory = new ConsoleLoggerFactory(false);
            var transport = new AzureServiceBusTransport(StandardAzureServiceBusTransportFactory.ConnectionString, QueueName, consoleLoggerFactory, new TplAsyncTaskFactory(consoleLoggerFactory));
            transport.PurgeInputQueue();
            //Create the queue for the receiver since it cannot create it self beacuse of lacking rights on the namespace
            transport.CreateQueue(QueueName);

            var recieverActivator = new BuiltinHandlerActivator();
            var senderActivator = new BuiltinHandlerActivator();

            var receiverBus = Configure.With(recieverActivator)
                .Logging(l => l.ColoredConsole())
                .Transport(t =>
                    t.UseAzureServiceBus(StandardAzureServiceBusTransportFactory.ConnectionString, QueueName)
                        .DoNotCreateQueues())
                .Start();

            var senderBus = Configure.With(senderActivator)
                .Transport(t => t.UseAzureServiceBus(StandardAzureServiceBusTransportFactory.ConnectionString, "sender", mode))
                .Start();

            Using(receiverBus);
            Using(senderBus);

            var gotMessage = new ManualResetEvent(false);

            recieverActivator.Handle<string>(async (bus, context, message) =>
            {
                gotMessage.Set();
                Console.WriteLine("got message in readonly mode");
            });
            await senderBus.Advanced.Routing.Send(QueueName, "message to receiver");

            gotMessage.WaitOrDie(TimeSpan.FromSeconds(10));


        }
    }

    public class NotCreatingQueueTest : FixtureBase
    {
        [TestCase(AzureServiceBusMode.Basic)]
        [TestCase(AzureServiceBusMode.Standard)]
        public void ShouldNotCreateInputQueueWhenConfiguredNotTo(AzureServiceBusMode mode)
        {
            var manager = NamespaceManager.CreateFromConnectionString(StandardAzureServiceBusTransportFactory.ConnectionString);
            var queueName = Guid.NewGuid().ToString("N");

            Assert.IsFalse(manager.QueueExists(queueName));

            var recieverActivator = new BuiltinHandlerActivator();
            var bus = Configure.With(recieverActivator)
                .Logging(l => l.ColoredConsole())
                .Transport(t =>
                    t.UseAzureServiceBus(StandardAzureServiceBusTransportFactory.ConnectionString, queueName, mode)
                        .DoNotCreateQueues())
                .Start();
           
                Assert.IsFalse(manager.QueueExists(queueName));
           
            Using(recieverActivator);
            Using(bus);

        }
    }
}
