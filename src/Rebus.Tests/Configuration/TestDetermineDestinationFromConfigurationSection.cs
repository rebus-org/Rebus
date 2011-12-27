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