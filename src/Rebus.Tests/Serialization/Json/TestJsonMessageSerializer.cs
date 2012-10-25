using System.Collections.Generic;
using NUnit.Framework;
using Rebus.Messages;
using Rebus.Serialization.Json;
using Rebus.Shared;
using Shouldly;

namespace Rebus.Tests.Serialization.Json
{
    [TestFixture]
    public class TestJsonMessageSerializer : FixtureBase
    {
        JsonMessageSerializer serializer;

        protected override void DoSetUp()
        {
            serializer = new JsonMessageSerializer();
        }

        [Test]
        public void IncludesContentTypeAndEncodingInProperHeaderWhenSerializing()
        {
            // arrange
            var message = CreateSemiComplexMessage();
            
            // act
            var transportMessage = serializer.Serialize(message);

            // assert
            transportMessage.Headers.ShouldContainKeyAndValue(Headers.ContentType, "text/json");
            transportMessage.Headers.ShouldContainKeyAndValue(Headers.Encoding, "utf-7");
        }

        [Test]
        public void CanDoCustomTypeResolveByResolvingToDifferentType()
        {
            // arrange
            serializer.AddTypeResolver(d =>
                {
                    if (d.TypeName == "Rebus.Tests.Serialization.Json.TestJsonMessageSerializer+SomeMessage")
                    {
                        return typeof (SomeDucktypinglyCompatibleMessage);
                    }
                    return null;
                });

            // act
            var message = RoundtripMessage(new SomeMessage { SomeField = "hello!" });

            // assert
            message.ShouldBeTypeOf<SomeDucktypinglyCompatibleMessage>();
            ((SomeDucktypinglyCompatibleMessage)message).SomeField.ShouldBe("hello!");
        }

        [Test]
        public void CanDoCustomTypeResolveByResolvingToDifferentName()
        {
            // arrange
            serializer.AddNameResolver(d =>
                {
                    if (d == typeof(SomeMessage))
                    {
                        return new TypeDescriptor("Rebus.Tests",
                            "Rebus.Tests.Serialization.Json.TestJsonMessageSerializer+SomeDucktypinglyCompatibleMessage");
                    }
                    return null;
                });

            // act
            var message = RoundtripMessage(new SomeMessage { SomeField = "hello!" });

            // assert
            message.ShouldBeTypeOf<SomeDucktypinglyCompatibleMessage>();
            ((SomeDucktypinglyCompatibleMessage)message).SomeField.ShouldBe("hello!");
        }

        object RoundtripMessage(object message)
        {
            var transportMessageToSend = serializer.Serialize(new Message {Messages = new object[] {message}});
            var message2 = serializer.Deserialize(new ReceivedTransportMessage { Body = transportMessageToSend.Body });
            return message2.Messages[0];
        }

        static Message CreateSemiComplexMessage()
        {
            return new Message
                       {
                           Headers = new Dictionary<string, object>
                                         {
                                             {"some-header", "some-value"},
                                         },
                           Messages = new object[]
                                          {
                                              new SomeMessage {SomeField = "abc"},
                                              new AnotherMessage {AnotherField = "def"},
                                          }
                       };
        }

        class SomeMessage
        {
            public string SomeField { get; set; }            
        }

        class SomeDucktypinglyCompatibleMessage
        {
            public string SomeField { get; set; }
        }

        class AnotherMessage
        {
            public string AnotherField { get; set; }            
        }
    }
}