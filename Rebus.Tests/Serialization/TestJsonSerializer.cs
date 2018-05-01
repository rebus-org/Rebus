using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NUnit.Framework;
using Rebus.Messages;
using Rebus.Tests.Contracts;
using JsonSerializer = Rebus.Serialization.Json.JsonSerializer;
#pragma warning disable 4014

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

        /*
         Initial:
Made 23133 iterations in 5s
Made 81106 iterations in 5s
Made 104869 iterations in 5s
Made 111535 iterations in 5s
Made 104705 iterations in 5s

        With type cache:
Made 44492 iterations in 5s
Made 284389 iterations in 5s
Made 286152 iterations in 5s
Made 276416 iterations in 5s
Made 274822 iterations in 5s

*/
        [Test]
        [Repeat(5)]
        public async Task MeasureRate()
        {
            var iterations = 0L;
            var keepRunning = true;

            Task.Run(async () =>
            {
                await Task.Delay(TimeSpan.FromSeconds(1));

                Volatile.Write(ref keepRunning, false);
            });

            var headers = new Dictionary<string, string>{};
            var message = new Message(headers, new RandomMessage("hello thereø how's it goin?"));
            var transportMessage = await _serializer.Serialize(message);

            while (Volatile.Read(ref keepRunning))
            {
                await _serializer.Deserialize(transportMessage);

                iterations++;
            }

            Console.WriteLine($"Made {iterations} iterations in 5s");
        }

        [Test]
        public async Task WorksWithoutFullTypeNameHandlingToo()
        {
            var simpleSerializer = new JsonSerializer(new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.None });
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