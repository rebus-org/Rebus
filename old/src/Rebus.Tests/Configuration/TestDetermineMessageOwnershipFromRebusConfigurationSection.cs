using System;
using System.Configuration;
using NUnit.Framework;
using Rebus.Configuration;
using Shouldly;

namespace Rebus.Tests.Configuration
{
    [TestFixture]
    public class TestDetermineMessageOwnershipFromRebusConfigurationSection : FixtureBase
    {
        DetermineMessageOwnershipFromRebusConfigurationSection service;

        [Test]
        public void CanDetermineSomeRandomMapping()
        {
            // arrange
            service = new DetermineMessageOwnershipFromRebusConfigurationSection();

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
            service = new DetermineMessageOwnershipFromRebusConfigurationSection();

            // act
            // assert
            var exception = Assert.Throws<InvalidOperationException>(() => service.GetEndpointFor(typeof (string)));
            exception.Message.ShouldContain("System.String");
        }

        [Test]
        public void CanDetermineNameOfInputQueue()
        {
            // arrange

            // act
            var section = (RebusConfigurationSection)ConfigurationManager.GetSection("rebus");

            // assert
            section.InputQueue.ShouldBe("this.is.my.input.queue");
        }

        [Test]
        public void CanDetermineNumberOfWorkers()
        {
            // arrange
            

            // act
            var section = (RebusConfigurationSection)ConfigurationManager.GetSection("rebus");

            // assert
            section.Workers.ShouldBe(2);
        }

        [Test]
        public void CanDetermineNumberOfWorkersFromExtensionMethod()
        {
            // arrange

            // act
            var workers = RebusConfigurationSection.GetConfigurationValueOrDefault(x => x.Workers, 0);

            // assert
            workers.ShouldBe(2);
        }
    }

    class SomeMessageType
    {
    }

    class AnotherMessageType
    {
    }
}