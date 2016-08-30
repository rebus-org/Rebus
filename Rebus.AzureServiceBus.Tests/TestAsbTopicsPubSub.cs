using System;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Rebus.Activation;
using Rebus.AzureServiceBus.Tests.Factories;
using Rebus.Config;
using Rebus.Extensions;
using Rebus.Tests;
using Rebus.Tests.Contracts;
using Rebus.Tests.Contracts.Extensions;

namespace Rebus.AzureServiceBus.Tests
{
    [TestFixture, Category(TestCategory.Azure)]
    public class TestAsbTopicsPubSub : FixtureBase
    {
        readonly string _inputQueueName1 = TestConfig.QueueName("pubsub1");
        readonly string _inputQueueName2 = TestConfig.QueueName("pubsub2");
        readonly string _inputQueueName3 = TestConfig.QueueName("pubsub3");
        readonly string _connectionString = StandardAzureServiceBusTransportFactory.ConnectionString;
        
        BuiltinHandlerActivator _bus1;
        BuiltinHandlerActivator _bus2;
        BuiltinHandlerActivator _bus3;

        protected override void SetUp()
        {
            StandardAzureServiceBusTransportFactory.DeleteTopic(typeof (string).GetSimpleAssemblyQualifiedName().ToValidAzureServiceBusEntityName());

            _bus1 = StartBus(_inputQueueName1);
            _bus2 = StartBus(_inputQueueName2);
            _bus3 = StartBus(_inputQueueName3);
        }

        [Test]
        public async Task PubSubSeemsToWork()
        {
            var gotString1 = new ManualResetEvent(false);
            var gotString2 = new ManualResetEvent(false);

            _bus1.Handle<string>(async str => gotString1.Set());
            _bus2.Handle<string>(async str => gotString2.Set());

            await _bus1.Bus.Subscribe<string>();
            await _bus2.Bus.Subscribe<string>();

            await Task.Delay(500);

            await _bus3.Bus.Publish("hello there!!!!");

            gotString1.WaitOrDie(TimeSpan.FromSeconds(2));
            gotString2.WaitOrDie(TimeSpan.FromSeconds(2));
        }

        [Test]
        public async Task PubSubSeemsToWorkAlsoWithUnsubscribe()
        {
            var gotString1 = new ManualResetEvent(false);
            var subscriber2GotTheMessage = false;

            _bus1.Handle<string>(async str => gotString1.Set());

            _bus2.Handle<string>(async str =>
            {
                subscriber2GotTheMessage = true;
            });

            await _bus1.Bus.Subscribe<string>();
            await _bus2.Bus.Subscribe<string>();

            await Task.Delay(500);

            await _bus2.Bus.Unsubscribe<string>();

            await Task.Delay(500);

            await _bus3.Bus.Publish("hello there!!!!");

            gotString1.WaitOrDie(TimeSpan.FromSeconds(2));

            Assert.That(subscriber2GotTheMessage, Is.False, "Didn't expect subscriber 2 to get the string since it was unsubscribed");
        }

        BuiltinHandlerActivator StartBus(string inputQueue)
        {
            var bus = Using(new BuiltinHandlerActivator());

            Configure.With(bus)
                .Transport(t => t.UseAzureServiceBus(_connectionString, inputQueue))
                .Start();

            return bus;
        }
    }
}