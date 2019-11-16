using NUnit.Framework;
using Rebus.Activation;
using Rebus.Bus;
using Rebus.Bus.Advanced;
using Rebus.Config;
using Rebus.DataBus.InMem;
using Rebus.Extensions;
using Rebus.Handlers;
using Rebus.Persistence.InMem;
using Rebus.Routing.TypeBased;
using Rebus.Tests.Contracts;
using Rebus.Tests.Contracts.Extensions;
using Rebus.Topic;
using Rebus.Transport.InMem;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Rebus.Tests.Topic
{
    [TestFixture]
    public class TestCustomTopicConventionIntegrationTest : FixtureBase
    {
        const int limit = 2048;

        InMemorySubscriberStore _subscriberStore;
        InMemNetwork _network;
        InMemDataStore _dataStore;
        const string SenderQueue = "sender.queue";
        const string ReceiverQueue = "receiver.queue";

        protected override void SetUp()
        {
            _subscriberStore = new InMemorySubscriberStore();
            _network = new InMemNetwork();
            _dataStore = new InMemDataStore();
        }


        IBus GetSender()
        {
            var activator = new BuiltinHandlerActivator();

            Using(activator);

            return Configure.With(activator)
                .Options(o => o.Register<ITopicNameConvention>(c => new PrefixTopicNameConvention()))
                .Transport(t => t.UseInMemoryTransport(_network, SenderQueue))
                .Subscriptions(s => s.StoreInMemory(_subscriberStore))
                .DataBus(d => d.StoreInMemory(_dataStore))
                .Routing(r =>
                {
                    r.TypeBased().Map<SimpleMessage>(ReceiverQueue);
                })
                .Start();
        }

        IBus GetReceiver(Func<SimpleMessage, Task> action)
        {
            var activator = new BuiltinHandlerActivator();

            Using(activator);

            activator.Handle<SimpleMessage>(action);

            return Configure.With(activator)
                .Options(o => o.Register<ITopicNameConvention>(c => new PrefixTopicNameConvention()))
                .Transport(t => t.UseInMemoryTransport(_network, ReceiverQueue))
                .Subscriptions(s => s.StoreInMemory(_subscriberStore))
                .DataBus(d => d.StoreInMemory(_dataStore))
                .Start();
        }


        public class PrefixTopicNameConvention : ITopicNameConvention
        {
            public string GetTopic(Type eventType)
            {
                return "PREFIX_" + eventType.GetSimpleAssemblyQualifiedName();
            }
        }

        private class SimpleMessage
        {
            public string Something { get; set; }
        }

        [Test]
        public async Task CorrectlySerializeAndDeserializeWhenUseCustomTopic()
        {
            var sendText = "TEST_TEST";
            string recivedText = null;
            var receivedBusReset = new ManualResetEvent(false);

            var sender = GetSender();
            var receiver = GetReceiver((SimpleMessage message) => {
                recivedText = message.Something;
                receivedBusReset.Set();
                return Task.FromResult(0);
            });

            await receiver.Subscribe<SimpleMessage>();

            //await sender.Send(new SimpleMessage() { Something = sendText });
            await sender.Publish(new SimpleMessage() { Something = sendText });

            receivedBusReset.WaitOrDie(TimeSpan.FromSeconds(2));

            Assert.AreEqual(sendText, recivedText);
        }


    }
}
