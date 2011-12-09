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
using System;
using NUnit.Framework;
using Rebus.Configuration;
using Shouldly;

namespace Rebus.Tests.Configuration
{
    [TestFixture]
    public class TestDetermineDestinationFromConfigurationSection : FixtureBase
    {
        DetermineDestinationFromConfigurationSection service;

        protected override void DoSetUp()
        {
            service = new DetermineDestinationFromConfigurationSection();
        }

        [Test]
        public void CanDetermineSomeRandomMapping()
        {
            // arrange

            // act
            var endpointForSomeMessageType = service.GetEndpointFor(typeof(SomeMessageType));
            var endpointForAnotherMessageType = service.GetEndpointFor(typeof(AnotherMessageType));

            // assert
            endpointForSomeMessageType.ShouldBe("some_message_endpoint");
            endpointForAnotherMessageType.ShouldBe("another_message_endpoint");
        }

        [Test]
        public void ThrowsWhenMappingCannotBeFound()
        {
            // arrange
            

            // act
            // assert
            var exception = Assert.Throws<InvalidOperationException>(() => service.GetEndpointFor(typeof (string)));
            exception.Message.ShouldContain("System.String");
        }
    }

    class SomeMessageType
    {
    }

    class AnotherMessageType
    {
    }
}