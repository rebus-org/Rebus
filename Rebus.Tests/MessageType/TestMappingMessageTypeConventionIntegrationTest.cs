using NUnit.Framework;
using Rebus.Activation;
using Rebus.Bus;
using Rebus.Config;
using Rebus.DataBus.InMem;
using Rebus.Handlers;
using Rebus.Messages.MessageType;
using Rebus.Persistence.InMem;
using Rebus.Routing.TypeBased;
using Rebus.Tests.Contracts;
using Rebus.Tests.Contracts.Extensions;
using Rebus.Transport.InMem;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Rebus.Tests.MessageType
{
    [TestFixture]
    public class TestMappingMessageTypeMapperIntegrationTest : FixtureBase
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
                .MessageTypes(t =>
                {
                    t.MapMessageType()
                        .Map<SimpleMessage>("simplemessage");
                })
                .Transport(t => t.UseInMemoryTransport(_network, SenderQueue))
                .Subscriptions(s => s.StoreInMemory(_subscriberStore))
                .DataBus(d => d.StoreInMemory(_dataStore))
                .Routing(r =>
                {
                    r.TypeBased().Map<SimpleMessage>(ReceiverQueue);
                })
                .Start();
        }

        IBus GetReceiver(Func<SimpleMessage2, Task> action)
        {
            var activator = new BuiltinHandlerActivator();

            Using(activator);

            activator.Handle<SimpleMessage2>(action);

            return Configure.With(activator)
                .MessageTypes(t =>
                {
                    t.MapMessageType()
                        .Map<SimpleMessage2>("simplemessage");
                })
                .Transport(t => t.UseInMemoryTransport(_network, ReceiverQueue))
                .Subscriptions(s => s.StoreInMemory(_subscriberStore))
                .DataBus(d => d.StoreInMemory(_dataStore))
                .Start();
        }

        private class SimpleMessage
        {
            public string Something { get; set; }
        }

        private class SimpleMessage2
        {
            public string Something { get; set; }
        }

        [Test]
        public async Task CorrectlySerializeAndDeserializeWhenUswMessageMapper()
        {
            var sendText = "TEST_TEST";
            string recivedText = null;
            var receivedBusReset = new ManualResetEvent(false);

            var sender = GetSender();
            var receiver = GetReceiver((SimpleMessage2 message) => {
                recivedText = message.Something;
                receivedBusReset.Set();
                return Task.FromResult(0);
            });

            await receiver.Subscribe<SimpleMessage2>();

            //await sender.Send(new SimpleMessage() { Something = sendText });
            await sender.Publish(new SimpleMessage() { Something = sendText });

            receivedBusReset.WaitOrDie(TimeSpan.FromSeconds(2));

            Assert.AreEqual(sendText, recivedText);
        }

    }
}
