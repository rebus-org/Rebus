using System;
using System.IO;
using NUnit.Framework;
using Rebus.Tests.Configuration;
using Rebus.Timeout.Configuration;
using Shouldly;

namespace Rebus.Tests.Timeout
{
    [TestFixture]
    public class TestTimeoutConfigurationSection
    {
        [Test]
        public void CanLoadConfigurationSection()
        {
            using (AppConfig.Change(GetPathOf("app.config.01.xml")))
            {
                var section = TimeoutConfigurationSection.GetSection();

                section.InputQueue.ShouldBe("rebus.timeout.input");
                section.ErrorQueue.ShouldBe("rebus.timeout.error");
                section.StorageType.ShouldBe("SQL");
                section.ConnectionString.ShouldBe("server=.;initial catalog=RebusTimeoutManager;integrated security=sspi");
                section.TableName.ShouldBe("timeouts");
            }
        }

        string GetPathOf(string fileName)
        {
            var testAppConfigPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"Timeout\AppConfigExamples", fileName);
            Assert.That(File.Exists(testAppConfigPath), "Test app config file {0} does not exist!", testAppConfigPath);
            return testAppConfigPath;
        }
    }
}