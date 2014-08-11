using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using NUnit.Framework;
using Rebus.Messages;
using Rebus.Serialization.Binary;
using Rebus.Serialization.Json;
using Shouldly;

namespace Rebus.Tests.Contracts.Serialization
{
    [TestFixture(typeof(JsonMessageSerializer))]
    [TestFixture(typeof(BinaryMessageSerializer))]
    public class TestSerialization<TSerializer> : FixtureBase where TSerializer : ISerializeMessages, new()
    {
        TSerializer instance;

        protected override void DoSetUp()
        {
            instance = new TSerializer();
        }

        [TestCase(typeof(TimeoutRequest))]
        [TestCase(typeof(TimeoutReply))]
        [TestCase(typeof(SubscriptionMessage))]
        public void CanSerializeRebusControlMessages(Type controlBusMessageType)
        {
            var messageInstance = Activator.CreateInstance(controlBusMessageType);

            var messageToSerialize = new Message
                                         {
                                             Headers = new Dictionary<string, object>(),
                                             Messages = new[] {messageInstance}
                                         };
            
            var transportMessageToSend = instance.Serialize(messageToSerialize);
            
            var deserializedMessage = instance.Deserialize(transportMessageToSend.ToReceivedTransportMessage());
        }

        [Test]
        public void CanSerializeComplexNestedType()
        {
            var transportMessageToSend = instance
                .Serialize(new Message
                    {
                        Headers = new Dictionary<string, object>
                            {
                                {"some_key", "some_value"},
                                {"another_key", "another_value"},
                            },
                        Messages = new object[]
                            {
                                "primitive string message",
                                new ComplexObject
                                    {
                                        NestedObjects = new List<NestedObject>
                                            {
                                                new NestedObject
                                                    {
                                                        IntProperty = 21,
                                                        StringProperty = "some string",
                                                        DateTimeProperty =
                                                            new DateTime(2006, 09, 11),
                                                        TimeSpanProperty =
                                                            new TimeSpan(14, 20, 00, 00),
                                                    }
                                            }
                                    }
                            }
                    });

            var message = instance.Deserialize(transportMessageToSend.ToReceivedTransportMessage());

            message.Headers.ShouldContainKeyAndValue("some_key", "some_value");
            message.Headers.ShouldContainKeyAndValue("another_key", "another_value");

            message.Messages.Length.ShouldBe(2);
            message.Messages[0].ShouldBe("primitive string message");
            message.Messages[1].ShouldBeOfType<ComplexObject>();

            var complexObject = (ComplexObject)message.Messages[1];
            complexObject.NestedObjects.Count.ShouldBe(1);
            complexObject.NestedObjects[0].IntProperty.ShouldBe(21);
            complexObject.NestedObjects[0].StringProperty.ShouldBe("some string");
            complexObject.NestedObjects[0].DateTimeProperty.ShouldBe(new DateTime(2006, 09, 11));
            complexObject.NestedObjects[0].TimeSpanProperty.ShouldBe(new TimeSpan(14, 20, 00, 00));
        }

        [Test]
        public void CanSerializeComplexTypeWithInheritance()
        {
            var person = new Person
                {
                    Address = new ForeignAddress
                        {
                            Lines = new[]
                                {
                                    "Torsmark 4", "8700 Horsens"
                                }
                        }
                };
            var transportMessageToSend = instance.Serialize(new Message { Messages = new object[] { person } });
            var message = instance.Deserialize(transportMessageToSend.ToReceivedTransportMessage());
            var deserializedPerson = (Person)message.Messages[0];

            deserializedPerson.Address.ShouldBeOfType<ForeignAddress>();
            var foreignAddress = (ForeignAddress)deserializedPerson.Address;
            foreignAddress.Lines[0].ShouldBe("Torsmark 4");
            foreignAddress.Lines[1].ShouldBe("8700 Horsens");
        }

        [Test, Description("Checks how many serialization operations that can be performed in one second")]
        public void CheckPerformance()
        {
            const int window = 1;

            var site = new Site
                           {
                               LocalUnits = new[] { "ali1", "ali2", "ali3", "ali4", "ali5" }
                                   .Select(CreateLocalUnit)
                                   .ToArray()
                           };

            var stopwatch = Stopwatch.StartNew();

            var iterations = 0;
            double elapsedSeconds;

            for (; (elapsedSeconds = stopwatch.Elapsed.TotalSeconds) < window; iterations++)
            {
                var transportMessageToSend = instance.Serialize(new Message { Messages = new object[] { site } });
                var message = instance.Deserialize(transportMessageToSend.ToReceivedTransportMessage());
                var deserializedSite = (Site)message.Messages[0];
                deserializedSite.LocalUnits.Length.ShouldBe(site.LocalUnits.Length);
            }

            Console.WriteLine("{0} iterations in {1:0.0} s", iterations, elapsedSeconds);
        }

        LocalUnit CreateLocalUnit(string alias)
        {
            return new LocalUnit
                       {
                           Alias = alias,
                           Settings = Enumerable.Range(0, 100).ToArray(),
                           MoreSettings = Enumerable.Range(0, 200)
                               .Select(i => i.ToString(CultureInfo.InvariantCulture))
                               .ToArray()
                       };
        }

        [Serializable]
        class Site
        {
            public LocalUnit[] LocalUnits { get; set; }
        }

        [Serializable]
        class LocalUnit
        {
            public string Alias { get; set; }
            public int[] Settings { get; set; }
            public string[] MoreSettings { get; set; }
        }

        [Serializable]
        class Person
        {
            public Address Address { get; set; }
        }

        [Serializable]
        abstract class Address
        {
        }

        [Serializable]
        class DomesticAddress : Address
        {
            public string Street { get; set; }
        }

        [Serializable]
        class ForeignAddress : Address
        {
            public string[] Lines { get; set; }
        }

        [Serializable]
        class ComplexObject
        {
            public List<NestedObject> NestedObjects { get; set; }
        }

        [Serializable]
        class NestedObject
        {
            public int IntProperty { get; set; }
            public string StringProperty { get; set; }
            public DateTime DateTimeProperty { get; set; }
            public TimeSpan TimeSpanProperty { get; set; }
        }
    }
}