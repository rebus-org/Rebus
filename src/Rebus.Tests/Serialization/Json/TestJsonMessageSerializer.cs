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

        class AnotherMessage
        {
            public string AnotherField { get; set; }            
        }
    }
}