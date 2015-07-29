using NUnit.Framework;
using Rebus.HttpGateway;
using Shouldly;

namespace Rebus.Tests.HttpGateway
{
    [TestFixture]
    public class TestRebusGatewayConfigurationSection : FixtureBase
    {
        [Test]
        public void CanReadGatewayConfigurationElement()
        {
            // arrange

            // act
            var section = RebusGatewayConfigurationSection.LookItUp();

            // assert
            var incomingSection = section.Inbound;
            incomingSection.ShouldNotBe(null);
            incomingSection.ListenUri.ShouldBe("http://+:8080");
            incomingSection.DestinationQueue.ShouldBe("test.rebus.incoming");

            var outgoingSection = section.Outbound;
            outgoingSection.ShouldNotBe(null);
            outgoingSection.ListenQueue.ShouldBe("test.rebus.outgoing");
            outgoingSection.DestinationUri.ShouldBe("http://localhost:8080");
        }
    }
}