using NUnit.Framework;
using Rebus.Retry.CircuitBreaker;
using System;
using System.Threading.Tasks;

namespace Rebus.Tests.Retry.CircuitBreaker
{
    [TestFixture]
    public class ExceptionTypeCircuitBreakerTests 
    {
        [Test]
        public void Trip_WithOneAttemptTrippedOnce_DoesOpenCircuit()
        {
            var sut = new ExceptionTypeCircuitBreaker(typeof(MyCustomException), new CircuitBreakerSettings(1, 2, 60));

            sut.Trip(new MyCustomException());

            Assert.That(sut.State, Is.EqualTo(CircuitBreakerState.Open));
        }

        [Test]
        public void Trip_WithTwoAttemptTrippedOnce_DoesNotOpenCircuit()
        {
            var sut = new ExceptionTypeCircuitBreaker(typeof(MyCustomException), new CircuitBreakerSettings(2, 60, 60));

            sut.Trip(new MyCustomException());

            Assert.That(sut.State, Is.EqualTo(CircuitBreakerState.Closed));
        }

        [Test]
        public async Task Trip_WithTwoAttemptTrippedTwice_OutSideTrackingPeriod_DoesNotOpenCircuit()
        {
            var sut = new ExceptionTypeCircuitBreaker(typeof(MyCustomException), new CircuitBreakerSettings(2, 2, 180));

            sut.Trip(new MyCustomException());
            await Task.Delay(TimeSpan.FromSeconds(3.1));
            sut.Trip(new MyCustomException());


            Assert.That(sut.State, Is.EqualTo(CircuitBreakerState.Closed));
        }

        [Test]
        public async Task Trip_WithTwoAttemptTrippedTwice_InsideTrackingPeriod_DoesOpenCircuit()
        {
            var sut = new ExceptionTypeCircuitBreaker(typeof(MyCustomException), new CircuitBreakerSettings(2, 60, 180));

            sut.Trip(new MyCustomException());
            await Task.Delay(TimeSpan.FromSeconds(2.5));
            sut.Trip(new MyCustomException());

            Assert.That(sut.State, Is.EqualTo(CircuitBreakerState.Open));
        }

        class MyCustomException : Exception
        {

        }
    }
}