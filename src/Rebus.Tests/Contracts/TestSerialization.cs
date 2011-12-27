using System;
using System.Collections.Generic;
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
            var instance = (ISerializeMessages) Activator.CreateInstance(serializerType);

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
                                                                                              DateTimeProperty = new DateTime(2006, 09, 11),
                                                                                              TimeSpanProperty = new TimeSpan(14, 20, 00, 00),
                                                                                          }
                                                                                  }
                                                          }
                                                  }
                               });

            var message = instance.Deserialize(new ReceivedTransportMessage {Data = transportMessageToSend.Data});

            message.Headers.Count.ShouldBe(2);
            message.Messages.Length.ShouldBe(2);
            message.Messages[0].ShouldBe("primitive string message");
            message.Messages[1].ShouldBeTypeOf<ComplexObject>();
            
            var complexObject = (ComplexObject) message.Messages[1];
            complexObject.NestedObjects.Count.ShouldBe(1);
            complexObject.NestedObjects[0].IntProperty.ShouldBe(21);
            complexObject.NestedObjects[0].StringProperty.ShouldBe("some string");
            complexObject.NestedObjects[0].DateTimeProperty.ShouldBe(new DateTime(2006, 09, 11));
            complexObject.NestedObjects[0].TimeSpanProperty.ShouldBe(new TimeSpan(14, 20, 00, 00));
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