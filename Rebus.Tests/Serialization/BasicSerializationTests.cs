using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NUnit.Framework;
using Rebus.DataBus;
using Rebus.Messages;
using Rebus.Messages.Control;
using Rebus.Serialization;

namespace Rebus.Tests.Serialization
{
    public class BasicSerializationTests<TSerializerFactory> : FixtureBase where TSerializerFactory : ISerializerFactory, new()
    {
        TSerializerFactory _factory;
        ISerializer _serializer;

        protected override void SetUp()
        {
            _factory = new TSerializerFactory();
            _serializer = _factory.GetSerializer();
        }

        [Test]
        public async Task CanRoundtripInternalMessages_DataBusAttachment()
        {
            var message = new DataBusAttachment("bimmelim!!!");

            Console.WriteLine("Roundtripping {0}", message.GetType());

            var roundtrippedMessage = (DataBusAttachment)await Roundtrip(message);

            Assert.That(roundtrippedMessage.Id, Is.EqualTo("bimmelim!!!"));
        }

        [Test]
        public async Task CanRoundtripInternalMessages_SubscribeRequest()
        {
            var message = new SubscribeRequest { Topic = "topic", SubscriberAddress = "address" };

            Console.WriteLine("Roundtripping {0}", message.GetType());

            var roundtrippedMessage = (SubscribeRequest)await Roundtrip(message);

            Assert.That(roundtrippedMessage.SubscriberAddress, Is.EqualTo(message.SubscriberAddress));
            Assert.That(roundtrippedMessage.Topic, Is.EqualTo(message.Topic));
        }

        [Test]
        public async Task CanRoundtripInternalMessages_UnsubscribeRequest()
        {
            var message = new UnsubscribeRequest { Topic = "topic", SubscriberAddress = "address" };

            Console.WriteLine("Roundtripping {0}", message.GetType());

            var roundtrippedMessage = (UnsubscribeRequest)await Roundtrip(message);

            Assert.That(roundtrippedMessage.SubscriberAddress, Is.EqualTo(message.SubscriberAddress));
            Assert.That(roundtrippedMessage.Topic, Is.EqualTo(message.Topic));
        }

        [TestCase(5)]
        [TestCase("hej")]
        [TestCase(56.5)]
        public async Task CanRoundtripSomeBasicValues(object originalMessage)
        {
            Console.WriteLine("Roundtripping {0}", originalMessage.GetType());

            var roundtrippedMessage = await Roundtrip(originalMessage);

            Assert.That(roundtrippedMessage, Is.EqualTo(originalMessage));
        }

        [Test]
        public async Task CanRoundtripSimpleObject()
        {
            const string text = "hej med dig min ven";

            var someMessage = new SomeMessage { Text = text };

            var someMessageRoundtripped = await Roundtrip(someMessage);

            Assert.That(someMessageRoundtripped, Is.TypeOf<SomeMessage>());
            Assert.That(((SomeMessage)someMessageRoundtripped).Text, Is.EqualTo(text));
        }

        async Task<object> Roundtrip(object o)
        {
            var message = new Message(new Dictionary<string, string>(), o);

            var transportMessage = await _serializer.Serialize(message);

            var roundtrippedMessage = await _serializer.Deserialize(transportMessage);

            return roundtrippedMessage.Body;
        }
    }

    public class SomeMessage
    {
        public string Text { get; set; }
    }
}