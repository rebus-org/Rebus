using System;
using NUnit.Framework;
using Rebus.Bus;
using Rebus.Tests.Integration;
using System.Linq;
using Shouldly;

namespace Rebus.Tests.Unit
{
    [TestFixture]
    public class TestErrorTracker : FixtureBase
    {
        ErrorTracker errorTracker;
        private TimeSpan timeoutSpan;

        protected override void DoSetUp()
        {
            TimeMachine.Reset();
            timeoutSpan = TimeSpan.FromDays(1);
            errorTracker = new ErrorTracker(timeoutSpan, TimeSpan.FromHours(10), "error");
        }

        protected override void DoTearDown()
        {
            errorTracker.Dispose();
        }

        [Test]
        public void KeepsOnly10MostRecentExceptionDetailsEvenThoughThereMayBeManyMoreFailedDeliveries()
        {
            // arrange
            const string messageId = "bim!";
            errorTracker.MaxRetries = 1000;

            // act
            2000.Times(() => errorTracker.TrackDeliveryFail(messageId, new OmfgExceptionThisIsBad("w00t!")));
            var messageHasFailedMaximumNumberOfTimes = errorTracker.MessageHasFailedMaximumNumberOfTimes(messageId);
            var info = errorTracker.GetPoisonMessageInfo(messageId);

            // assert
            messageHasFailedMaximumNumberOfTimes.ShouldBe(true);
            info.Exceptions.Count().ShouldBe(10);
        }

        [Test]
        public void ErrorTrackerRemovesAMessageWhichTimedOut()
        {
            //Arrange
            const string messageId = "testId";
            var fakeTime = RebusTimeMachine.Now();
            TimeMachine.FixTo(fakeTime);

            //Act
            errorTracker.TrackDeliveryFail(messageId, new Exception());
            errorTracker.TrackDeliveryFail(messageId, new Exception());

            TimeMachine.FixTo(fakeTime.Add(timeoutSpan));

            errorTracker.CheckForMessageTimeout();

            var errorText = errorTracker.GetErrorText(messageId);

            //Assert
            Assert.That(errorText, Is.Empty);
        }


        [Test]
        public void ErrorTrackerRemovesMultipleMessagesWhichTimedOut()
        {
            //Arrange
            const string messageId = "testId";
            const string messageId2 = "testId2";
            var fakeTime = RebusTimeMachine.Now();
            TimeMachine.FixTo(fakeTime);

            //Act
            errorTracker.TrackDeliveryFail(messageId, new Exception());
            TimeMachine.FixTo(fakeTime.Add(TimeSpan.FromMinutes(10)));
            errorTracker.TrackDeliveryFail(messageId2, new Exception());
            TimeMachine.FixTo(fakeTime.AddDays(1).AddMinutes(10));

            errorTracker.CheckForMessageTimeout();

            var errorText1 = errorTracker.GetErrorText(messageId);
            var errorText2 = errorTracker.GetErrorText(messageId2);

            //Assert
            Assert.That(errorText1, Is.Empty);
            Assert.That(errorText2, Is.Empty);
        }

        [Test]
        public void ErrorTrackerDoesntRemoveMessageWhichHasntTimedOut()
        {
            //Arrange
            const string messageId = "testId";
            const string messageId2 = "testId2";
            var fakeTime = DateTime.UtcNow;

            //Act
            TimeMachine.FixTo(fakeTime);
            errorTracker.TrackDeliveryFail(messageId, new Exception(string.Format("This exception occurred at {0}", RebusTimeMachine.Now())));

            TimeMachine.FixTo(fakeTime + TimeSpan.FromMinutes(10));
            errorTracker.TrackDeliveryFail(messageId2, new Exception(string.Format("This exception occurred at {0}", RebusTimeMachine.Now())));

            TimeMachine.FixTo(fakeTime + TimeSpan.FromDays(1) + TimeSpan.FromMinutes(5));
            errorTracker.CheckForMessageTimeout();

            var errorText1 = errorTracker.GetErrorText(messageId);
            var errorText2 = errorTracker.GetErrorText(messageId2);

            //Assert
            Assert.That(errorText1, Is.Empty);
            Assert.That(errorText2, Is.Not.Empty);
        }
    }
}
