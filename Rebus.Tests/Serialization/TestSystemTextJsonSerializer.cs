using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NUnit.Framework;
using Rebus.Extensions;
using Rebus.Messages;
using Rebus.Serialization;
using Rebus.Serialization.Custom;
using Rebus.Serialization.Json;
using Rebus.Tests.Contracts;
using JsonSerializer = Rebus.Serialization.Json.JsonSerializer;
#pragma warning disable 4014

namespace Rebus.Tests.Serialization;

[TestFixture]
public class TestSystemTextJsonSerializer : FixtureBase
{
    SystemTextJsonSerializer _serializer;

    protected override void SetUp()
    {
        _serializer = new SystemTextJsonSerializer(new SimpleAssemblyQualifiedMessageTypeNameConvention());
    }

    /*
     Initial:
Made 23133 iterations in 00:00:01
Made 81106 iterations in 00:00:01
Made 104869 iterations in 00:00:01
Made 111535 iterations in 00:00:01
Made 104705 iterations in 00:00:01

    With type cache:
Made 44492 iterations in 00:00:01
Made 284389 iterations in 00:00:01
Made 286152 iterations in 00:00:01
Made 276416 iterations in 00:00:01
Made 274822 iterations in 00:00:01


    On Dell box at the office:
Made 373190 iterations in 00:00:01
Made 394550 iterations in 00:00:01
Made 426973 iterations in 00:00:01
Made 436008 iterations in 00:00:01
Made 434064 iterations in 00:00:01

*/
    [Test]
    [Repeat(5)]
    public async Task MeasureRate()
    {
        var iterations = 0L;
        var keepRunning = true;
        var duration = TimeSpan.FromSeconds(1);

        Task.Run(async () =>
        {
            await Task.Delay(duration);

            Volatile.Write(ref keepRunning, false);
        });

        var headers = new Dictionary<string, string>();
        var message = new Message(headers, new RandomMessage("hello thereø how's it goin?"));
        var transportMessage = await _serializer.Serialize(message);

        while (Volatile.Read(ref keepRunning))
        {
            await _serializer.Deserialize(transportMessage);

            iterations++;
        }

        Console.WriteLine($"Made {iterations} iterations in {duration}");
    }

    [Test]
    public void CheckErrorWhenContentTypeHeaderIsMissing()
    {
        var jsonBody = Encoding.UTF8.GetBytes("{}");
        var headersWithEmptyContentType = new Dictionary<string, string> { [Headers.ContentType] = null };
        var headersWithoutContentType = new Dictionary<string, string>();

        var ex1 = Assert.ThrowsAsync<KeyNotFoundException>(() => _serializer.Deserialize(
            new TransportMessage(headersWithoutContentType, jsonBody)));

        Console.WriteLine(ex1);

        var ex2 = Assert.ThrowsAsync<FormatException>(() => _serializer.Deserialize(
            new TransportMessage(headersWithEmptyContentType, jsonBody)));

        Console.WriteLine(ex2);
    }

    [Test]
    [Description("Serializer would wrongly use its own current encoding when default encoding was detected")]
    public async Task CheckEncodingBug()
    {
        var utf32Serializer = new SystemTextJsonSerializer(new SimpleAssemblyQualifiedMessageTypeNameConvention(), encoding: Encoding.UTF32);
        var utf8Serializer = new SystemTextJsonSerializer(new SimpleAssemblyQualifiedMessageTypeNameConvention(), encoding: Encoding.UTF8);

        var transportMessage = await utf8Serializer.Serialize(new Message(new Dictionary<string, string>(), new Something("hej")));
        var roundtripped = await utf32Serializer.Deserialize(transportMessage);

        var something = roundtripped.Body as Something ?? throw new AssertionException($"Message body {roundtripped.Body} was not Something");

        Assert.That(something.Text, Is.EqualTo("hej"));
    }

    record Something(string Text);

    [Test]
    public async Task FormatTypeAsExpected_Default()
    {
        var expectedTypeName = typeof(SomeType).GetSimpleAssemblyQualifiedName();
        var serializer = new JsonSerializer(new SimpleAssemblyQualifiedMessageTypeNameConvention());

        var message = new Message(new Dictionary<string, string>(), new SomeType());
        var transportMessage = await serializer.Serialize(message);

        var type = transportMessage.Headers.GetValue(Headers.Type);

        Console.WriteLine($@"

Serialized type name: {type}
  Expected type name: {expectedTypeName}

");

        var roundtrippedMessage = await serializer.Deserialize(transportMessage);

        Assert.That(type, Is.EqualTo(expectedTypeName));
        Assert.That(roundtrippedMessage.Body, Is.TypeOf<SomeType>());
    }

    [Test]
    public async Task FormatTypeAsExpected_CustomWithFallback()
    {
        var serializer = new JsonSerializer(new CustomTypeNameConventionBuilder().AllowFallbackToDefaultConvention().GetConvention());

        var expectedTypeName = typeof(SomeType).GetSimpleAssemblyQualifiedName();

        var message = new Message(new Dictionary<string, string>(), new SomeType());
        var transportMessage = await serializer.Serialize(message);

        var type = transportMessage.Headers.GetValue(Headers.Type);

        Console.WriteLine($@"

Serialized type name: {type}
  Expected type name: {expectedTypeName}

");

        var roundtrippedMessage = await serializer.Deserialize(transportMessage);

        Assert.That(type, Is.EqualTo(expectedTypeName));
        Assert.That(roundtrippedMessage.Body, Is.TypeOf<SomeType>());
    }

    [Test]
    public async Task FormatTypeAsExpected_Custom()
    {
        var serializer = new JsonSerializer(new CustomTypeNameConventionBuilder()
            .AddWithShortName<SomeType>()
            .GetConvention());

        const string expectedTypeName = "SomeType";

        var message = new Message(new Dictionary<string, string>(), new SomeType());
        var transportMessage = await serializer.Serialize(message);

        var type = transportMessage.Headers.GetValue(Headers.Type);

        Console.WriteLine($@"

Serialized type name: {type}
  Expected type name: {expectedTypeName}

");

        var roundtrippedMessage = await serializer.Deserialize(transportMessage);

        Assert.That(type, Is.EqualTo(expectedTypeName));
        Assert.That(roundtrippedMessage.Body, Is.TypeOf<SomeType>());
    }

    class SomeType { }

    [Test]
    public async Task WorksWithoutFullTypeNameHandlingToo()
    {
        var simpleSerializer = new JsonSerializer(new SimpleAssemblyQualifiedMessageTypeNameConvention(), new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.None });
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

        var exception = Assert.Throws<FormatException>(() =>
        {
            _serializer.Deserialize(transportMessage).Wait();
        });

        Console.WriteLine(exception);
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