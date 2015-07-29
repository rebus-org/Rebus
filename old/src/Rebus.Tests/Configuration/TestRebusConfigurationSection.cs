using System;
using System.IO;
using NUnit.Framework;
using Rebus.Configuration;
using Shouldly;

namespace Rebus.Tests.Configuration
{
    [TestFixture]
    public class TestRebusConfigurationSection : FixtureBase
    {
        [Test]
        public void CanReadAuditQueueAttribute()
        {
            using (AppConfig.Change(GetPathOf("app.3.config")))
            {
                var section = RebusConfigurationSection.LookItUp();

                section.InputQueue.ShouldBe("input");
                section.ErrorQueue.ShouldBe("error");
                section.AuditQueue.ShouldBe("audit");
            }
        }

        [Test]
        public void CanReadSection()
        {
            using (AppConfig.Change(GetPathOf("app.1.config")))
            {
                var section = RebusConfigurationSection.LookItUp();

                section.InputQueue.ShouldBe("this.is.my.input.queue");
                section.ErrorQueue.ShouldBe("this.is.my.error.queue");
                section.TimeoutManagerAddress.ShouldBe("somewhere_else");
                section.Workers.ShouldBe(5);
                section.MaxRetries.ShouldBe(6);

                section.Address.ShouldBe("10.0.0.9");

                var rijndaelSection = section.RijndaelSection;
                rijndaelSection.ShouldNotBe(null);
                rijndaelSection.Key.ShouldBe("oA/ZUnFsR9w1qEatOByBSXc4woCuTxmR99tAuQ56Qko=");
            }
        }

        [Test]
        public void CanReadSection_AlsoWorksWhenRijndaelSectionIsOmitted()
        {
            using (AppConfig.Change(GetPathOf("app.2.config")))
            {
                var section = RebusConfigurationSection.LookItUp();

                section.InputQueue.ShouldBe("input");
                section.ErrorQueue.ShouldBe("error");
                section.Workers.ShouldBe(1);

                var rijndaelSection = section.RijndaelSection;
                rijndaelSection.Key.ShouldBe("");
            }
        }

        string GetPathOf(string testAppConfigFileName)
        {
            var testAppConfigPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"Configuration\RealAppConfigs", testAppConfigFileName);
            Assert.That(File.Exists(testAppConfigPath), "Test app config file {0} does not exist!", testAppConfigPath);
            return testAppConfigPath;
        }
    }
}