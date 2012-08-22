using System;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using Rebus.Configuration;
using Shouldly;

namespace Rebus.Tests.Configuration
{
    [TestFixture]
    public class TestRebusConfigurationSection : FixtureBase
    {
        [Test]
        public void CanReadSection()
        {
            using (AppConfig.Change(GetPathOf("app.1.config")))
            {
                var section = RebusConfigurationSection.LookItUp();

                section.InputQueue.ShouldBe("this.is.my.input.queue");
                section.ErrorQueue.ShouldBe("this.is.my.error.queue");
                section.Workers.ShouldBe(5);
                section.MaxRetries.ShouldBe(6);

                section.Address.ShouldBe("10.0.0.9");

                var rijndaelSection = section.RijndaelSection;
                rijndaelSection.ShouldNotBe(null);
                rijndaelSection.Key.ShouldBe("oA/ZUnFsR9w1qEatOByBSXc4woCuTxmR99tAuQ56Qko=");
            }
        }

        string GetPathOf(string testAppConfigFileName)
        {
            var testAppConfigPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"Configuration\RealAppConfigs", testAppConfigFileName);
            Assert.That(File.Exists(testAppConfigPath), "Test app config file {0} does not exist!", testAppConfigPath);
            return testAppConfigPath;
        }

        /// <summary>
        /// Awesome hack, found here: http://stackoverflow.com/a/6151688/6560
        /// </summary>
        public abstract class AppConfig : IDisposable
        {
            public static AppConfig Change(string path)
            {
                return new ChangeAppConfig(path);
            }

            public abstract void Dispose();

            private class ChangeAppConfig : AppConfig
            {
                private readonly string oldConfig =
                    AppDomain.CurrentDomain.GetData("APP_CONFIG_FILE").ToString();

                private bool disposedValue;

                public ChangeAppConfig(string path)
                {
                    AppDomain.CurrentDomain.SetData("APP_CONFIG_FILE", path);
                    ResetConfigMechanism();
                }

                public override void Dispose()
                {
                    if (!disposedValue)
                    {
                        AppDomain.CurrentDomain.SetData("APP_CONFIG_FILE", oldConfig);
                        ResetConfigMechanism();


                        disposedValue = true;
                    }
                    GC.SuppressFinalize(this);
                }

                private static void ResetConfigMechanism()
                {
                    typeof(ConfigurationManager)
                        .GetField("s_initState", BindingFlags.NonPublic | BindingFlags.Static)
                        .SetValue(null, 0);

                    typeof(ConfigurationManager)
                        .GetField("s_configSystem", BindingFlags.NonPublic | BindingFlags.Static)
                        .SetValue(null, null);

                    typeof(ConfigurationManager)
                        .Assembly.GetTypes()
                        .Where(x => x.FullName == "System.Configuration.ClientConfigPaths")
                        .First()
                        .GetField("s_current", BindingFlags.NonPublic | BindingFlags.Static)
                        .SetValue(null, null);
                }
            }
        }
    }
}