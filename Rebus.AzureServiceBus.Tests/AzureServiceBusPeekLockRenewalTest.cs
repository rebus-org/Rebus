using System;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Rebus.Activation;
using Rebus.Bus;
using Rebus.Config;
using Rebus.Tests;
using Rebus.Tests.Extensions;
using Rebus.Transport;

namespace Rebus.AzureServiceBus.Tests
{
    [TestFixture]
    public class AzureServiceBusPeekLockRenewalTest : FixtureBase
    {
        static readonly string QueueName = TestConfig.QueueName("input");

        BuiltinHandlerActivator _activator;
        IBus _bus;

        protected override void SetUp()
        {
            _activator = new BuiltinHandlerActivator();

            _bus = Configure.With(_activator)
                .Transport(t => t.UseAzureServiceBus(AzureServiceBusTransportFactory.ConnectionString, QueueName))
                .Options(o =>
                {
                    o.SetNumberOfWorkers(1);
                    o.SetMaxParallelism(1);
                })
                .Start();

            Using(_bus);
        }

        [Test]
        public async Task ItWorks()
        {
            var gotMessage = new ManualResetEvent(false);

            _activator.Handle<string>(async str =>
            {
                Console.WriteLine("waiting 6 minutes....");

                // longer than the longest asb peek lock in the world...
                await Task.Delay(TimeSpan.FromSeconds(6));

                Console.WriteLine("done waiting");

                gotMessage.Set();
            });

            await _bus.SendLocal("hej med dig min ven!");

            gotMessage.WaitOrDie(TimeSpan.FromMinutes(10));

            // shut down bus
            CleanUpDisposables();

            // see if queue is empty
            var transport = new AzureServiceBusTransport(AzureServiceBusTransportFactory.ConnectionString, QueueName);

            using (var transactionContext = new DefaultTransactionContext())
            {
                var message = await transport.Receive(transactionContext);

                Assert.That(message, Is.Null);

                transactionContext.Complete();
            }
        }
    }
}