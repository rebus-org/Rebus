using System;
using System.IO;
using NUnit.Framework;
using Rebus.HttpGateway;
using Shouldly;

namespace Rebus.Tests.HttpGateway.Http
{
    [TestFixture]
    public class TestRebusGatewayConfigurationSection : FixtureBase
    {
        [Test]
        public void CanReadGatewayConfigurationElement()
        {
            using (AppConfig.Change(ConfigFile("App01.config")))
            {
                // arrange

                // act
                var section = RebusGatewayConfigurationSection.LookItUp();

                // assert
                var incomingSection = section.Inbound;
                incomingSection.ShouldNotBe(null);
                incomingSection.ListenUri.ShouldBe("http://+:9005");
                incomingSection.DestinationQueue.ShouldBe("test.rebus.incoming");

                var outgoingSection = section.Outbound;
                outgoingSection.ShouldNotBe(null);
                outgoingSection.ListenQueue.ShouldBe("test.rebus.outgoing");
                outgoingSection.DestinationUri.ShouldBe("http://localhost:8081");
            }
        }

        static string ConfigFile(string fileName)
        {
            return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "HttpGateway", "Http",
                                "ConfigFiles", fileName);
        }
    }
}