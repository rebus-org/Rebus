// Copyright 2011 Mogens Heller Grabe
// 
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in
// compliance with the License. You may obtain a copy of the License at
// 
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software distributed under the License is
// distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and limitations under the License.

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
    }
}