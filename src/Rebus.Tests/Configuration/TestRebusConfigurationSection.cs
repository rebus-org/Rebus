using NUnit.Framework;
using Rebus.Configuration;
using Shouldly;

namespace Rebus.Tests.Configuration
{
    [TestFixture]
    public class TestRebusConfigurationSection
    {
        [Test]
        public void CanReadSection()
        {
            var section = RebusConfigurationSection.LookItUp();

            section.InputQueue.ShouldBe("this.is.my.input.queue");
            section.ErrorQueue.ShouldBe("this.is.my.error.queue");
            section.Workers.ShouldBe(5);

            var rijndaelSection = section.RijndaelSection;
            rijndaelSection.ShouldNotBe(null);
            rijndaelSection.Iv.ShouldBe("OLYKdaDyETlu7NbDMC45dA==");
            rijndaelSection.Key.ShouldBe("oA/ZUnFsR9w1qEatOByBSXc4woCuTxmR99tAuQ56Qko=");
        }
    }
}