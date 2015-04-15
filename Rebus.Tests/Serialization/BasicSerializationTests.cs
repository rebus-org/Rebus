using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NUnit.Framework;
using Rebus.Messages;
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

            var someMessage = new SomeMessage{Text = text};

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

    public interface ISerializerFactory
    {
        ISerializer GetSerializer();
    }
}