using System;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Rebus.Activation;
using Rebus.AzureServiceBus.Config;
using Rebus.AzureServiceBus.Tests.Factories;
using Rebus.Bus;
using Rebus.Config;
using Rebus.Logging;
using Rebus.Routing.TypeBased;
using Rebus.Tests;
using Rebus.Tests.Extensions;
using Rebus.Threading.TaskParallelLibrary;

namespace Rebus.AzureServiceBus.Tests
{
    [TestFixture, Category(TestCategory.Azure)]
    public class BasicAzureServiceBusBasicReceiveOnly : FixtureBase
    {
        readonly AzureServiceBusMode _azureServiceBusMode;
        static readonly string QueueName = TestConfig.QueueName("input");
        IBus _receiverBus;
        IBus _senderBus;
        AzureServiceBusTransport _transport;
        BuiltinHandlerActivator _recieverActivator;
        BuiltinHandlerActivator _senderActivator;
        protected override void SetUp()
        {
            var consoleLoggerFactory = new ConsoleLoggerFactory(false);
            _transport = new AzureServiceBusTransport(StandardAzureServiceBusTransportFactory.ConnectionString, QueueName, consoleLoggerFactory, new TplAsyncTaskFactory(consoleLoggerFactory));
            _transport.PurgeInputQueue();
            //Create the queue for the receiver since it cannot create it self beacuse of lacking rights on the namespace
            _transport.CreateQueue(QueueName);

            _recieverActivator = new BuiltinHandlerActivator();
            _senderActivator = new BuiltinHandlerActivator();

            _receiverBus = Configure.With(_recieverActivator)
                    .Logging(l => l.ColoredConsole())
                    .Transport(t => t.UseAzureServiceBusAsReadOnly(StandardAzureServiceBusTransportFactory.ConnectionString, QueueName))
                    .Start();

            _senderBus = Configure.With(_senderActivator)
                .Transport(t => t.UseAzureServiceBus(StandardAzureServiceBusTransportFactory.ConnectionString, "sender", AzureServiceBusMode.Basic))
                .Start();

            Using(_receiverBus);
            Using(_senderBus);
        }

        [Test]
        [ExpectedException(typeof(NotSupportedException), ExpectedMessage = "Not able to send. The bus is configured as receiveonly")]
        public async Task ShouldNotBeAbleToSend()
        {
            await _receiverBus.Advanced.Routing.Send("test", "message");
        }

        [Test]
        public async void ShouldBeAbleToRecieve()
        {
            var gotMessage = new ManualResetEvent(false);

            _recieverActivator.Handle<string>(async (bus, context, message) =>
            {
                gotMessage.Set();
                Console.WriteLine("got message in readonly mode");
            });
            await _senderBus.Advanced.Routing.Send(QueueName, "message to receiver");

            gotMessage.WaitOrDie(TimeSpan.FromSeconds(10));


        }
    }
}