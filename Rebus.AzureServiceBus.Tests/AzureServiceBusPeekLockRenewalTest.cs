using System;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Rebus.Activation;
using Rebus.AzureServiceBus.Config;
using Rebus.AzureServiceBus.Tests.Factories;
using Rebus.Bus;
using Rebus.Config;
using Rebus.Extensions;
using Rebus.Logging;
using Rebus.Messages;
using Rebus.Tests;
using Rebus.Tests.Extensions;
using Rebus.Threading;
using Rebus.Threading.TaskBased;
using Rebus.Transport;

namespace Rebus.AzureServiceBus.Tests
{
    [TestFixture(AzureServiceBusMode.Basic), Category(TestCategory.Azure)]
    [TestFixture(AzureServiceBusMode.Standard), Category(TestCategory.Azure)]
    public class AzureServiceBusPeekLockRenewalTest : FixtureBase
    {
        readonly AzureServiceBusMode _azureServiceBusMode;
        static readonly string QueueName = TestConfig.QueueName("input");

        BuiltinHandlerActivator _activator;
        IBus _bus;
        AzureServiceBusTransport _transport;

        public AzureServiceBusPeekLockRenewalTest(AzureServiceBusMode azureServiceBusMode)
        {
            _azureServiceBusMode = azureServiceBusMode;
        }

        protected override void SetUp()
        {
            var consoleLoggerFactory = new ConsoleLoggerFactory(false);
            _transport = new AzureServiceBusTransport(StandardAzureServiceBusTransportFactory.ConnectionString, QueueName, consoleLoggerFactory, new TplAsyncTaskFactory(consoleLoggerFactory));
            _transport.PurgeInputQueue();

            _activator = new BuiltinHandlerActivator();

            _bus = Configure.With(_activator)
                .Transport(t => t.UseAzureServiceBus(StandardAzureServiceBusTransportFactory.ConnectionString, QueueName, _azureServiceBusMode)
                .AutomaticallyRenewPeekLock())
                .Options(o =>
                {
                    o.SetNumberOfWorkers(1);
                    o.SetMaxParallelism(1);
                })
                .Start();

            Using(_bus);
        }

        [Test, Ignore("Can be used to check silencing behavior when receive errors occur")]
        public void ReceiveExceptions()
        {
            Thread.Sleep(TimeSpan.FromMinutes(10));
        }

        [Test]
        public async Task ItWorks()
        {
            var gotMessage = new ManualResetEvent(false);

            _activator.Handle<string>(async str =>
            {
                Console.WriteLine("waiting 6 minutes....");

                // longer than the longest asb peek lock in the world...
                //await Task.Delay(TimeSpan.FromSeconds(3));
                await Task.Delay(TimeSpan.FromMinutes(6));

                Console.WriteLine("done waiting");

                gotMessage.Set();
            });

            await _bus.SendLocal("hej med dig min ven!");

            gotMessage.WaitOrDie(TimeSpan.FromMinutes(6.5));

            // shut down bus
            CleanUpDisposables();

            // see if queue is empty
            using (var transactionContext = new DefaultTransactionContext())
            {
                var message = await _transport.Receive(transactionContext);

                if (message != null)
                {
                    throw new AssertionException(string.Format("Did not expect to receive a message - got one with ID {0}", message.Headers.GetValue(Headers.MessageId)));    
                }

                await transactionContext.Complete();
            }
        }
    }
}