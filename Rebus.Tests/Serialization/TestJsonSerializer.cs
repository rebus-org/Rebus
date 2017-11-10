using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NUnit.Framework;
using Rebus.Messages;
using Rebus.Tests.Contracts;
using JsonSerializer = Rebus.Serialization.Json.JsonSerializer;

namespace Rebus.Tests.Serialization
{
    [TestFixture]
    public class TestJsonSerializer : FixtureBase
    {
        JsonSerializer _serializer;

        protected override void SetUp()
        {
            _serializer = new JsonSerializer();
        }

        [Test]
        public async Task WorksWithoutFullTypeNameHandlingToo()
        {
            var simpleSerializer = new JsonSerializer(new JsonSerializerSettings());
            var message = new RandomMessage("hei allihoppa");
            var transportMessage = await simpleSerializer.Serialize(new Message(new Dictionary<string, string>(), message));
            var roundtrippedMessage = (await simpleSerializer.Deserialize(transportMessage)).Body;
            Assert.That(roundtrippedMessage, Is.TypeOf<RandomMessage>());
        }

        class RandomMessage
        {
            public RandomMessage(string greeting)
            {
                Greeting = greeting;
            }

            public string Greeting { get; }
        }

        [Test]
        public async Task CutsLongJsonIncludedInDeserializationExceptionIfItIsTooLong()
        {
            var embeddedObjects = Enumerable.Range(0, 300)
                .Select(n => new EmbeddedObject($"HEJ MED DIG MIN VEN - DET HER ER BESKED {n}"));

            var someMessage = new SomeMessage(embeddedObjects.ToList());

            var headers = new Dictionary<string, string>();
            var message = new Message(headers, someMessage);

            var transportMessage = await _serializer.Serialize(message);

            var jsonText = Encoding.UTF8.GetString(transportMessage.Body);

            Console.WriteLine();
            Console.WriteLine($"JSON text length: {jsonText.Length}");
            Console.WriteLine();

            BreakMessage(transportMessage);

            var aggregateException = Assert.Throws<AggregateException>(() =>
            {
                _serializer.Deserialize(transportMessage).Wait();
            });

            Console.WriteLine(aggregateException);
        }

        static void BreakMessage(TransportMessage transportMessage)
        {
            for (var index = 1000; index < 2000; index++)
            {
                transportMessage.Body[index] = 84;
            }
        }

        class SomeMessage
        {
            public SomeMessage(List<EmbeddedObject> objects)
            {
                Objects = objects;
            }

            public List<EmbeddedObject> Objects { get; }
        }

        class EmbeddedObject
        {
            public EmbeddedObject(string text)
            {
                Text = text;
            }

            public string Text { get; }
        }
    }
}