using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using Rebus.DataBus;
using Rebus.Messages;
using Rebus.Messages.Control;
using Rebus.Serialization;
using Rebus.Tests.Contracts.Serialization.Default;

namespace Rebus.Tests.Contracts.Serialization;

/// <summary>
/// Test fixture base class for verifying compliance with the <see cref="ISerializer"/> contract
/// </summary>
public abstract class BasicSerializationTests<TSerializerFactory> : FixtureBase where TSerializerFactory : ISerializerFactory, new()
{
    TSerializerFactory _factory;
    ISerializer _serializer;

    protected override void SetUp()
    {
        _factory = new TSerializerFactory();
        _serializer = _factory.GetSerializer();
    }

    /*
Results after informally testing # roundtrips in one second with each serializer:

Completed 1710 roundtrips in 1 s with Rebus.Serialization
That's 1710 roundtrips/s

Completed 3922 roundtrips in 1 s with Rebus.Jil
That's 3922 roundtrips/s

Completed 7855 roundtrips in 1 s with Rebus.Wire
That's 7855 roundtrips/s

Completed 11224 roundtrips in 1 s with Rebus.Protobuf
That's 11224 roundtrips/s

     */
    [TestCase(1)]
    public void CountNumberOfObjectRoundtrips(int numberOfSeconds)
    {
        var testTime = TimeSpan.FromSeconds(numberOfSeconds);

        var objectContainer = new RootObject
        {
            BigObjects = Enumerable
                .Range(0, 100)
                .Select(i => new BigObject
                {
                    Integer = i,
                    String = $"This is string number {i}"
                })
                .ToList()
        };

        var headersDictionary = new Dictionary<string, string>();

        var message = new Message(headersDictionary, objectContainer);

        // warm up
        var dummy = _serializer.Deserialize(_serializer.Serialize(message).Result).Result;

        var stopwatch = Stopwatch.StartNew();
        var roundtrips = 0;
        while (true)
        {
            var result = _serializer.Deserialize(_serializer.Serialize(message).Result).Result;
            roundtrips++;

            if (stopwatch.Elapsed > testTime) break;
        }
        stopwatch.Stop();

        Console.WriteLine($@"Completed {roundtrips} roundtrips in {numberOfSeconds} s with {_serializer.GetType().Namespace}
That's {roundtrips / (double)numberOfSeconds:0.#} roundtrips/s");
    }

    [Test]
    public void HeadersAreCopied()
    {
        var originalHeaders = new Dictionary<string, string>();
        var message = new Message(originalHeaders, "hej");

        var transportMessage = _serializer.Serialize(message).Result;
        var transportMessageHeaders = transportMessage.Headers;

        var roundtrippedMessage = _serializer.Deserialize(transportMessage).Result;
        var roundtrippedMessageHeaders = roundtrippedMessage.Headers;

        originalHeaders["key"] = "HEJ!";
        transportMessageHeaders["key"] = "MED!";
        roundtrippedMessageHeaders["key"] = "DIG!";

        Assert.That(originalHeaders["key"], Is.EqualTo("HEJ!"));
        Assert.That(transportMessageHeaders["key"], Is.EqualTo("MED!"));
        Assert.That(roundtrippedMessageHeaders["key"], Is.EqualTo("DIG!"));
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
    [TestCase(56.5f)]
    public async Task CanRoundtripSomeBasicValues(object originalMessage)
    {
        try
        {
            Console.WriteLine("Roundtripping {0}", originalMessage.GetType());

            var roundtrippedMessage = await Roundtrip(originalMessage);

            Assert.That(roundtrippedMessage, Is.EqualTo(originalMessage));
        }
        catch (NotSupportedException)
        {
            Console.WriteLine($"Serialization of '{originalMessage.GetType()}' instance is not supported by {_serializer.GetType()}");
        }
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