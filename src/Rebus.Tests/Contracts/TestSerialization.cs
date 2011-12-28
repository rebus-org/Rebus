using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using NUnit.Framework;
using Rebus.Messages;
using Rebus.Newtonsoft.JsonNET;
using Rebus.Serialization.Binary;
using Shouldly;

namespace Rebus.Tests.Contracts
{
    [TestFixture]
    public class TestSerialization
    {
        [TestCase(typeof(JsonMessageSerializer))]
        [TestCase(typeof(BinaryMessageSerializer))]
        public void ItWorks(Type serializerType)
        {
            CheckSerializationOfSimpleComplexType(serializerType);
            CheckSerializationOfComplexTypesWithInheritance(serializerType);
            CheckPerformance(serializerType);
        }

        void CheckPerformance(Type serializerType)
        {
            var instance = GetInstance(serializerType);

            var site = new Site
                           {
                               LocalUnits = new[] {"ali1", "ali2", "ali3", "ali4", "ali5"}
                                   .Select(CreateLocalUnit)
                                   .ToArray()
                           };

            var stopwatch = Stopwatch.StartNew();
            var count = 1000;
            count.Times(() =>
                            {
                                var transportMessageToSend = instance.Serialize(new Message { Messages = new object[] { site } });
                                var message = instance.Deserialize(new ReceivedTransportMessage { Data = transportMessageToSend.Data });
                                var deserializedSite = (Site)message.Messages[0];
                                deserializedSite.LocalUnits.Length.ShouldBe(site.LocalUnits.Length);
                            });

            var totalSeconds = stopwatch.Elapsed.TotalSeconds;
            var serializerName = serializerType.FullName ?? "";
            Console.WriteLine(@"{0}
{1}
{2} roundtrips took {3:0.0} s - that's {4:0}/s",
                              serializerName,
                              new string('-', serializerName.Length),
                              count,
                              totalSeconds,
                              count/totalSeconds);
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

        void CheckSerializationOfComplexTypesWithInheritance(Type serializerType)
        {
            var instance = GetInstance(serializerType);

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
            var transportMessageToSend = instance.Serialize(new Message {Messages = new object[] {person}});
            var message = instance.Deserialize(new ReceivedTransportMessage { Data = transportMessageToSend.Data });
            var deserializedPerson = (Person) message.Messages[0];

            deserializedPerson.Address.ShouldBeTypeOf<ForeignAddress>();
            var foreignAddress = (ForeignAddress) deserializedPerson.Address;
            foreignAddress.Lines[0].ShouldBe("Torsmark 4");
            foreignAddress.Lines[1].ShouldBe("8700 Horsens");
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

        void CheckSerializationOfSimpleComplexType(Type serializerType)
        {
            var instance = GetInstance(serializerType);

            var transportMessageToSend = instance
                .Serialize(new Message
                               {
                                   Headers = new Dictionary<string, string>
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

            var message = instance.Deserialize(new ReceivedTransportMessage { Data = transportMessageToSend.Data });

            message.Headers.Count.ShouldBe(2);
            message.Messages.Length.ShouldBe(2);
            message.Messages[0].ShouldBe("primitive string message");
            message.Messages[1].ShouldBeTypeOf<ComplexObject>();

            var complexObject = (ComplexObject)message.Messages[1];
            complexObject.NestedObjects.Count.ShouldBe(1);
            complexObject.NestedObjects[0].IntProperty.ShouldBe(21);
            complexObject.NestedObjects[0].StringProperty.ShouldBe("some string");
            complexObject.NestedObjects[0].DateTimeProperty.ShouldBe(new DateTime(2006, 09, 11));
            complexObject.NestedObjects[0].TimeSpanProperty.ShouldBe(new TimeSpan(14, 20, 00, 00));
        }

        static ISerializeMessages GetInstance(Type serializerType)
        {
            var instance = (ISerializeMessages)Activator.CreateInstance(serializerType);
            return instance;
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