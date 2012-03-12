using System;
using System.Collections.Generic;
using NUnit.Framework;
using Rebus.Serialization;
using Shouldly;

namespace Rebus.Tests.Serialization
{
    [TestFixture]
    public class TestDictionarySerializer : FixtureBase
    {
        DictionarySerializer serializer;

        protected override void DoSetUp()
        {
            serializer = new DictionarySerializer();
        }

        [Test]
        public void CanRoundtripException()
        {
            // generate "authentic" exception with stack trace and everything :)
            string toSerialize;
            try
            {
                throw new ApplicationException("uh oh, something bad has happened!");
            }
            catch (Exception e)
            {
                toSerialize = e.ToString().Replace(Environment.NewLine, "|");
            }

            var dictionaryWithExceptionMessages = new Dictionary<string, string> {{"exception-message", toSerialize}};
            var str = serializer.Serialize(dictionaryWithExceptionMessages);

            Console.WriteLine(@"

here it is:

{0}

", str);

            var deserializedDictionary = serializer.Deserialize(str);
            deserializedDictionary.ShouldBe(dictionaryWithExceptionMessages);
        }

        [Test]
        public void CanRoundtripSimpleStuff()
        {
            var dictionaryWithExceptionMessages = new Dictionary<string, string>
                                                      {
                                                          { "exception-message", "woohoo simple stuff works!" },
                                                          { "exception-message2", "woohoo simple stuff works!" },
                                                      };
            var str = serializer.Serialize(dictionaryWithExceptionMessages);
            var deserializedDictionary = serializer.Deserialize(str);
            deserializedDictionary.ShouldBe(dictionaryWithExceptionMessages);
        }
    }
}