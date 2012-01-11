using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Rebus.Transports.Msmq;
using Shouldly;

namespace Rebus.Tests.Msmq
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
        public void YieldsNullWhenDeserializingEmptyString()
        {
            serializer.Deserialize("").ShouldBe(null);
            serializer.Deserialize(null).ShouldBe(null);
            serializer.Deserialize("    ").ShouldBe(null);
        }

        [Test]
        public void CanSerializeEmptyDictionary()
        {
            var str = serializer.Serialize(new Dictionary<string, string>());
            str.ShouldBe("[]");
            var dict = serializer.Deserialize(str);
            dict.Count.ShouldBe(0);
        }

        [Test]
        public void CanSerializeSimpleValue()
        {
            var str = serializer.Serialize(new Dictionary<string, string> {{"greeting", "HELLO!"}});
            str.ShouldBe(@"[{""greeting"",""HELLO!""}]");
            var dict = serializer.Deserialize(str);
            dict.Count.ShouldBe(1);
            var list = dict.ToList();
            list[0].Key.ShouldBe("greeting");
            list[0].Value.ShouldBe("HELLO!");
        }

        [Test]
        public void CanSerializeThreeValues()
        {
            var str = serializer.Serialize(new Dictionary<string, string>
                                               {
                                                   {"first", "w00t!"},
                                                   {"second", "w00t!!!1"},
                                                   {"thirrrrd", "ZOMG!!"},
                                               });
            str.ShouldBe(@"[{""first"",""w00t!""};{""second"",""w00t!!!1""};{""thirrrrd"",""ZOMG!!""}]");
            var dict = serializer.Deserialize(str);
            var list = dict.ToList();
            
            list.Count.ShouldBe(3);
            
            list[0].Key.ShouldBe("first");
            list[1].Key.ShouldBe("second");
            list[2].Key.ShouldBe("thirrrrd");
            
            list[0].Value.ShouldBe("w00t!");
            list[1].Value.ShouldBe("w00t!!!1");
            list[2].Value.ShouldBe("ZOMG!!");
        }

        [TestCase("value containing ,")]
        [TestCase("value containing ;")]
        [TestCase("value containing {")]
        [TestCase("value containing }")]
        public void ThrowsIfStringContainsInvalidValud(string str)
        {
            var dictionaryWithInvalidKey = new Dictionary<string, string> {{str, "some value"}};
            var dictionaryWithInvalidValue = new Dictionary<string, string> {{str, "some value"}};
            
            Assert.Throws<FormatException>(() => serializer.Serialize(dictionaryWithInvalidKey), "Did not expect serialization of dictionary with invalid KEY to succeed");
            Assert.Throws<FormatException>(() => serializer.Serialize(dictionaryWithInvalidValue), "Did not expect serialization of dictionary with invalid VALUE to succeed");
        }
    }
}