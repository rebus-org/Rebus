using System;
using NUnit.Framework;
using Rebus.Exceptions;
using Rebus.Time;

namespace Rebus.Tests.Exceptions
{
    [TestFixture]
    public class TestIgnorant
    {
        [Test]
        public void DoesNotLogFirstError()
        {
            var ignorant = new Ignorant
            {
                SilencePeriods = new[] { TimeSpan.FromMinutes(1) }
            };

            var isToBeIgnored = ignorant.IsToBeIgnored(new ApplicationException("hej"));

            Assert.That(isToBeIgnored, Is.True);
        }

        [Test]
        public void LogsAfterSilencePeriodIsOver()
        {
            var now = DateTime.UtcNow;

            var ignorant = new Ignorant
            {
                SilencePeriods = new[] { TimeSpan.FromMinutes(1) }
            };

            RebusTimeMachine.FakeIt(now);
            var first = ignorant.IsToBeIgnored(new ApplicationException("hej"));
            
            RebusTimeMachine.FakeIt(now + TimeSpan.FromMinutes(1.1));
            var second = ignorant.IsToBeIgnored(new ApplicationException("hej"));

            Assert.That(first, Is.True);
            Assert.That(second, Is.False);
        }

        [Test]
        public void GoesOnToNextSilencePeriodAfterTheFirstHasElapsed()
        {
            var now = DateTime.UtcNow;

            var ignorant = new Ignorant
            {
                SilencePeriods = new[]
                {
                    TimeSpan.FromMinutes(1),
                    TimeSpan.FromMinutes(10)
                }
            };

            RebusTimeMachine.FakeIt(now);
            var first = ignorant.IsToBeIgnored(new ApplicationException("hej"));
            
            RebusTimeMachine.FakeIt(now + TimeSpan.FromMinutes(1.1));
            var second = ignorant.IsToBeIgnored(new ApplicationException("hej"));

            RebusTimeMachine.FakeIt(now + TimeSpan.FromMinutes(11.1));
            var third = ignorant.IsToBeIgnored(new ApplicationException("hej"));

            RebusTimeMachine.FakeIt(now + TimeSpan.FromMinutes(11.2));
            var fourth = ignorant.IsToBeIgnored(new ApplicationException("hej"));

            RebusTimeMachine.FakeIt(now + TimeSpan.FromMinutes(21.2));
            var fifth = ignorant.IsToBeIgnored(new ApplicationException("hej"));

            RebusTimeMachine.FakeIt(now + TimeSpan.FromMinutes(21.3));
            var sixth = ignorant.IsToBeIgnored(new ApplicationException("hej"));

            Assert.That(first, Is.True);
            Assert.That(second, Is.False);
            Assert.That(third, Is.True);
            Assert.That(fourth, Is.False);
            Assert.That(fifth, Is.True);
            Assert.That(sixth, Is.False);
        }

        [Test]
        public void ResetsTimeAfterLogging()
        {
            var now = DateTime.UtcNow;

            var ignorant = new Ignorant
            {
                SilencePeriods = new[] { TimeSpan.FromMinutes(1) }
            };

            RebusTimeMachine.FakeIt(now);
            var first = ignorant.IsToBeIgnored(new ApplicationException("hej"));

            RebusTimeMachine.FakeIt(now + TimeSpan.FromMinutes(1.1));
            var second = ignorant.IsToBeIgnored(new ApplicationException("hej"));
            var third = ignorant.IsToBeIgnored(new ApplicationException("hej"));

            Assert.That(first, Is.True);
            Assert.That(second, Is.False);
            Assert.That(third, Is.True);
        }

        [Test]
        public void ResetsWhenResetIsCalled()
        {
            var now = DateTime.UtcNow;

            var ignorant = new Ignorant
            {
                SilencePeriods = new[] { TimeSpan.FromMinutes(1) }
            };

            RebusTimeMachine.FakeIt(now);
            var first = ignorant.IsToBeIgnored(new ApplicationException("hej"));

            RebusTimeMachine.FakeIt(now + TimeSpan.FromMinutes(0.9));
            var second = ignorant.IsToBeIgnored(new ApplicationException("hej"));

            ignorant.Reset();
            RebusTimeMachine.FakeIt(now + TimeSpan.FromMinutes(1.1));
            var third = ignorant.IsToBeIgnored(new ApplicationException("hej"));

            Assert.That(first  , Is.True);
            Assert.That(second  , Is.True);
            Assert.That(third  , Is.True);
        }
    }
}