using System;
using Rebus.Exceptions;
using Rebus.Time;
using Xunit;

namespace Rebus.Tests.Exceptions
{
    public class TestIgnorant
    {
        [Fact]
        public void DoesNotLogFirstError()
        {
            var ignorant = new Ignorant
            {
                SilencePeriods = new[] { TimeSpan.FromMinutes(1) }
            };

            var isToBeIgnored = ignorant.IsToBeIgnored(new Exception("hej"));

            Assert.True(isToBeIgnored);
        }

        [Fact]
        public void LogsAfterSilencePeriodIsOver()
        {
            var now = DateTime.UtcNow;

            var ignorant = new Ignorant
            {
                SilencePeriods = new[] { TimeSpan.FromMinutes(1) }
            };

            RebusTimeMachine.FakeIt(now);
            var first = ignorant.IsToBeIgnored(new Exception("hej"));
            
            RebusTimeMachine.FakeIt(now + TimeSpan.FromMinutes(1.1));
            var second = ignorant.IsToBeIgnored(new Exception("hej"));

            Assert.True(first);
            Assert.False(second);
        }

        [Fact]
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
            var first = ignorant.IsToBeIgnored(new Exception("hej"));
            
            RebusTimeMachine.FakeIt(now + TimeSpan.FromMinutes(1.1));
            var second = ignorant.IsToBeIgnored(new Exception("hej"));

            RebusTimeMachine.FakeIt(now + TimeSpan.FromMinutes(11.1));
            var third = ignorant.IsToBeIgnored(new Exception("hej"));

            RebusTimeMachine.FakeIt(now + TimeSpan.FromMinutes(11.2));
            var fourth = ignorant.IsToBeIgnored(new Exception("hej"));

            RebusTimeMachine.FakeIt(now + TimeSpan.FromMinutes(21.2));
            var fifth = ignorant.IsToBeIgnored(new Exception("hej"));

            RebusTimeMachine.FakeIt(now + TimeSpan.FromMinutes(21.3));
            var sixth = ignorant.IsToBeIgnored(new Exception("hej"));

            Assert.True(first);
            Assert.False(second);
            Assert.True(third);
            Assert.False(fourth);
            Assert.True(fifth);
            Assert.False(sixth);
        }

        [Fact]
        public void ResetsTimeAfterLogging()
        {
            var now = DateTime.UtcNow;

            var ignorant = new Ignorant
            {
                SilencePeriods = new[] { TimeSpan.FromMinutes(1) }
            };

            RebusTimeMachine.FakeIt(now);
            var first = ignorant.IsToBeIgnored(new Exception("hej"));

            RebusTimeMachine.FakeIt(now + TimeSpan.FromMinutes(1.1));
            var second = ignorant.IsToBeIgnored(new Exception("hej"));
            var third = ignorant.IsToBeIgnored(new Exception("hej"));

            Assert.True(first);
            Assert.False(second);
            Assert.True(third);
        }

        [Fact]
        public void ResetsWhenResetIsCalled()
        {
            var now = DateTime.UtcNow;

            var ignorant = new Ignorant
            {
                SilencePeriods = new[] { TimeSpan.FromMinutes(1) }
            };

            RebusTimeMachine.FakeIt(now);
            var first = ignorant.IsToBeIgnored(new Exception("hej"));

            RebusTimeMachine.FakeIt(now + TimeSpan.FromMinutes(0.9));
            var second = ignorant.IsToBeIgnored(new Exception("hej"));

            ignorant.Reset();
            RebusTimeMachine.FakeIt(now + TimeSpan.FromMinutes(1.1));
            var third = ignorant.IsToBeIgnored(new Exception("hej"));

            Assert.True(first);
            Assert.True(second);
            Assert.True(third);
        }
    }
}