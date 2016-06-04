using System;
using System.Threading.Tasks;
using NUnit.Framework;
using Rebus.Sagas.Locking;
using Rebus.Tests.Extensions;

namespace Rebus.Tests.Contracts.Locks
{
    public abstract class PessimisticLocksTest<TFactory> : FixtureBase where TFactory : IPessimisticLockerFactory, new()
    {
        IPessimisticLocker _locker;
        TFactory _factory;

        protected override void SetUp()
        {
            _factory = new TFactory();
            _locker = _factory.Create();
        }

        [Test]
        public void NothingHappensWhenReleasingLockThatHasNotBeenGrabbed()
        {
            100.Times(() => _locker.Release(Guid.NewGuid().ToString()).Wait());
        }

        [Test]
        public async Task CanGrabAndReleaseLock()
        {
            var result = await _locker.TryAcquire("lock");

            await _locker.Release("lock");

            Assert.That(result, Is.True);
        }

        [Test]
        public async Task CannotAcquireLockTwice()
        {
            var firstResult = await _locker.TryAcquire("lock");

            var secondResult = await _locker.TryAcquire("lock");
            var thirdResult = await _locker.TryAcquire("lock");

            Assert.That(firstResult, Is.True);
            Assert.That(secondResult, Is.False);
            Assert.That(thirdResult, Is.False);
        }

        [Test]
        public async Task ReleasedLockCanBeAcquired()
        {
            await _locker.TryAcquire("lock");
            await _locker.Release("lock");

            var result = await _locker.TryAcquire("lock");

            Assert.That(result, Is.True);
        }
    }
}