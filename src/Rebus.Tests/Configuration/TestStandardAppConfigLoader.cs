using System;
using NUnit.Framework;
using Rebus.Configuration;

namespace Rebus.Tests.Configuration
{
    [TestFixture]
    public class TestStandardAppConfigLoader : FixtureBase
    {
        StandardAppConfigLoader loader;

        protected override void DoSetUp()
        {
            loader = new StandardAppConfigLoader();
        }

        [Test]
        public void CanLoadTestAppConfig()
        {
            Console.WriteLine(loader.LoadIt());
        }
    }
}