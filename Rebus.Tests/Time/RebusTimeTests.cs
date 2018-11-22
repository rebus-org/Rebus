using System;
using NUnit.Framework;
using Rebus.Time;

namespace Rebus.Tests.Time
{
    [TestFixture]
    public class RebusTimeTests
    {
        [SetUp]
        public void SetUp()
        {
            RebusTime.Reset();
        }
        
        [Test]
        public void Now_RetrievesNowByDefault()
        {
            var before = DateTimeOffset.Now;
            var now = RebusTime.Now;
            var after = DateTimeOffset.Now;

            Assert.That(now, Is.InRange(before, after));
        }

        [Test]
        public void SetFactory_OverridesNow()
        {
            var value = new DateTimeOffset(1990, 03, 27, 12, 34, 56, TimeSpan.FromHours(1));
            RebusTime.SetFactory(() => value);

            Assert.That(RebusTime.Now, Is.EqualTo(value));
        }

        [Test]
        public void SetFactory_Null_ThrowsExceptionAndDoesNotSetFactory()
        {
            Assert.That(() => RebusTime.SetFactory(null), Throws.ArgumentNullException);
            Now_RetrievesNowByDefault();
        }

        [Test]
        public void Reset_ResetsFactoryToDefault()
        {
            RebusTime.SetFactory(() => new DateTimeOffset(1990, 03, 27, 12, 34, 56, TimeSpan.FromHours(1)));
            RebusTime.Reset();
            
            Now_RetrievesNowByDefault();
        }

        [TearDown]
        public void TearDown()
        {
            RebusTime.Reset();
        }
    }
}
