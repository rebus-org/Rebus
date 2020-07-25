using NUnit.Framework;
using Rebus.Logging;
using Rebus.Retry.CircuitBreaker;
using System;
using System.Collections.Generic;

namespace Rebus.Tests.Retry.CircuitBreaker
{
    [TestFixture]
    public class MainCircuitBreakerTests 
    {
        [Test]
        public void State_AllClosed_ReturnsClosed() 
        {
            var sut = new MainCircuitBreaker(new List<ICircuitBreaker>()
            {
                new FakeCircuitBreaker(CircuitBreakerState.Closed),
                new FakeCircuitBreaker(CircuitBreakerState.Closed),
                new FakeCircuitBreaker(CircuitBreakerState.Closed),
            }, new ConsoleLoggerFactory(false), null);

            Assert.That(sut.State, Is.EqualTo(CircuitBreakerState.Closed));
            Assert.IsTrue(sut.IsClosed);
            Assert.IsFalse(sut.IsHalfOpen);
            Assert.IsFalse(sut.IsOpen);
        }

        [Test]
        public void State_OneHalfOpen_ReturnsHalfOpen()
        {
            var sut = new MainCircuitBreaker(new List<ICircuitBreaker>()
            {
                new FakeCircuitBreaker(CircuitBreakerState.Closed),
                new FakeCircuitBreaker(CircuitBreakerState.HalfOpen),
                new FakeCircuitBreaker(CircuitBreakerState.Closed),
            }, new ConsoleLoggerFactory(false), null);

            Assert.That(sut.State, Is.EqualTo(CircuitBreakerState.HalfOpen));
            Assert.IsFalse(sut.IsClosed);
            Assert.IsTrue(sut.IsHalfOpen);
            Assert.IsFalse(sut.IsOpen);
        }

        [Test]
        public void State_OneOpen_ReturnsOpen()
        {
            var sut = new MainCircuitBreaker(new List<ICircuitBreaker>()
            {
                new FakeCircuitBreaker(CircuitBreakerState.Closed),
                new FakeCircuitBreaker(CircuitBreakerState.Closed),
                new FakeCircuitBreaker(CircuitBreakerState.Open),
            }, new ConsoleLoggerFactory(false), null);

            Assert.That(sut.State, Is.EqualTo(CircuitBreakerState.Open));
            Assert.IsFalse(sut.IsClosed);
            Assert.IsFalse(sut.IsHalfOpen);
            Assert.IsTrue(sut.IsOpen);
        }

        [Test]
        public void State_MixedBag_ReturnsOpen()
        {
            var sut = new MainCircuitBreaker(new List<ICircuitBreaker>()
            {
                new FakeCircuitBreaker(CircuitBreakerState.HalfOpen),
                new FakeCircuitBreaker(CircuitBreakerState.Closed),
                new FakeCircuitBreaker(CircuitBreakerState.Open),
            }, new ConsoleLoggerFactory(false), null);

            Assert.That(sut.State, Is.EqualTo(CircuitBreakerState.Open));
            Assert.IsFalse(sut.IsClosed);
            Assert.IsFalse(sut.IsHalfOpen);
            Assert.IsTrue(sut.IsOpen);
        }

        internal class FakeCircuitBreaker : ICircuitBreaker
        {
            public FakeCircuitBreaker(CircuitBreakerState state)
            {
                this.State = state;
            }

            public CircuitBreakerState State { get; private set; }

            public bool IsClosed => State == CircuitBreakerState.Closed;

            public bool IsHalfOpen => State == CircuitBreakerState.HalfOpen;

            public bool IsOpen => State == CircuitBreakerState.Open;

            public void Trip(Exception exception)
            {
            }
        }
    }
}