using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Rebus.DataBus;
using Rebus.Messages;
using Rebus.Messages.Control;
using Rebus.Serialization;
using Rebus.Tests.Contracts.Extensions;
using Rebus.Tests.Contracts.Serialization.Default;
using Xunit;

namespace Rebus.Tests.Contracts.Serialization
{
    /// <summary>
    /// Test fixture base class for verifying compliance with the <see cref="ISerializer"/> contract
    /// </summary>
    public class BasicSerializationTests<TSerializerFactory> : FixtureBase where TSerializerFactory : ISerializerFactory, new()
    {
        TSerializerFactory _factory;
        ISerializer _serializer;

        public BasicSerializationTests()
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
        [Theory]
        [InlineData(1)]
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

        [Fact]
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

            Assert.Equal("HEJ!", originalHeaders["key"]);
            Assert.Equal("MED!", transportMessageHeaders["key"]);
            Assert.Equal("DIG!", roundtrippedMessageHeaders["key"]);
        }

        [Fact]
        public async Task CanRoundtripInternalMessages_DataBusAttachment()
        {
            var message = new DataBusAttachment("bimmelim!!!");

            Console.WriteLine("Roundtripping {0}", message.GetType());

            var roundtrippedMessage = (DataBusAttachment)await Roundtrip(message);

            Assert.Equal("bimmelim!!!", roundtrippedMessage.Id);
        }

        [Fact]
        public async Task CanRoundtripInternalMessages_SubscribeRequest()
        {
            var message = new SubscribeRequest { Topic = "topic", SubscriberAddress = "address" };

            Console.WriteLine("Roundtripping {0}", message.GetType());

            var roundtrippedMessage = (SubscribeRequest)await Roundtrip(message);

            Assert.Equal(message.SubscriberAddress, roundtrippedMessage.SubscriberAddress);
            Assert.Equal(message.Topic, roundtrippedMessage.Topic);
        }

        [Fact]
        public async Task CanRoundtripInternalMessages_UnsubscribeRequest()
        {
            var message = new UnsubscribeRequest { Topic = "topic", SubscriberAddress = "address" };

            Console.WriteLine("Roundtripping {0}", message.GetType());

            var roundtrippedMessage = (UnsubscribeRequest)await Roundtrip(message);

            Assert.Equal(message.SubscriberAddress, roundtrippedMessage.SubscriberAddress);
            Assert.Equal(roundtrippedMessage.Topic, message.Topic);
        }

        [Theory]
        [InlineData(5)]
        [InlineData("hej")]
        [InlineData(56.5)]
        [InlineData(56.5f)]
        public async Task CanRoundtripSomeBasicValues(object originalMessage)
        {
            try
            {
                Console.WriteLine("Roundtripping {0}", originalMessage.GetType());

                var roundtrippedMessage = await Roundtrip(originalMessage);

                // added the ToString() as the equality comparison was failing due to single being compared to a double, ...
                Assert.Equal(originalMessage.ToString(), roundtrippedMessage.ToString());
            }
            catch (NotSupportedException)
            {
                Console.WriteLine($"Serialization of '{originalMessage.GetType()}' instance is not supported by {_serializer.GetType()}");
            }
        }

        [Fact]
        public async Task CanRoundtripSimpleObject()
        {
            const string text = "hej med dig min ven";

            var someMessage = new SomeMessage { Text = text };

            var someMessageRoundtripped = await Roundtrip(someMessage);

            Assert.IsType<SomeMessage>(someMessageRoundtripped);
            Assert.Equal(text, ((SomeMessage)someMessageRoundtripped).Text);
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