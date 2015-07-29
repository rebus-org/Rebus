using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Rebus.Bus;
using Shouldly;

namespace Rebus.Tests.Unit
{
    [TestFixture]
    public class TestBackoffHelper : FixtureBase
    {
        [Test]
        public void ThrowsWhenInitializedWithNoElements()
        {
            Assert.Throws<ArgumentException>(() => new BackoffHelper(Enumerable.Empty<TimeSpan>()));
        }

        [Test]
        public void ThrowsWhenInitializedWithZeroTime()
        {
            Assert.Throws<ArgumentException>(() => new BackoffHelper(new []{TimeSpan.FromSeconds(0)}));
        }

        [Test]
        public void ThrowsWhenInitializedWithNegativeTime()
        {
            Assert.Throws<ArgumentException>(() => new BackoffHelper(new []{TimeSpan.FromSeconds(-10)}));
        }

        [Test]
        public void WaitsForAsLongAsTheGivenBackoffTimesSpecify()
        {
            var timesWaited = new List<TimeSpan>();
            var backoffTimes = new []
                               {
                                   TimeSpan.FromSeconds(1), 
                                   TimeSpan.FromSeconds(2),
                                   TimeSpan.FromSeconds(3),
                               };
            var helper = new BackoffHelper(backoffTimes)
                         {
                             waitAction = timesWaited.Add
                         };

            helper.Wait();
            helper.Wait();
            helper.Wait();

            timesWaited.Count.ShouldBe(3);
            timesWaited[0].ShouldBe(TimeSpan.FromSeconds(1));
            timesWaited[1].ShouldBe(TimeSpan.FromSeconds(2));
            timesWaited[2].ShouldBe(TimeSpan.FromSeconds(3));
        }

        [Test]
        public void KeepsWaitingForAsLongAsTheLastTimeSpanSpecifies()
        {
            var timesWaited = new List<TimeSpan>();
            var backoffTimes = new []
                               {
                                   TimeSpan.FromSeconds(1), 
                                   TimeSpan.FromSeconds(2),
                               };
            var helper = new BackoffHelper(backoffTimes)
                         {
                             waitAction = timesWaited.Add
                         };

            helper.Wait();
            helper.Wait();
            helper.Wait();
            helper.Wait();
            helper.Wait();

            timesWaited.Count.ShouldBe(5);
            timesWaited[2].ShouldBe(TimeSpan.FromSeconds(2));
            timesWaited[3].ShouldBe(TimeSpan.FromSeconds(2));
            timesWaited[4].ShouldBe(TimeSpan.FromSeconds(2));
        }

        [Test]
        public void CanBeReset()
        {
            var timesWaited = new List<TimeSpan>();
            var backoffTimes = new[]
                               {
                                   TimeSpan.FromSeconds(1), 
                                   TimeSpan.FromSeconds(2),
                               };
            var helper = new BackoffHelper(backoffTimes)
            {
                waitAction = timesWaited.Add
            };

            helper.Wait();
            helper.Wait();
            
            helper.Reset();

            helper.Wait();
            helper.Wait();

            timesWaited.Count.ShouldBe(4);
            timesWaited[0].ShouldBe(TimeSpan.FromSeconds(1));
            timesWaited[1].ShouldBe(TimeSpan.FromSeconds(2));
            timesWaited[2].ShouldBe(TimeSpan.FromSeconds(1));
            timesWaited[3].ShouldBe(TimeSpan.FromSeconds(2));
        }
    }
}