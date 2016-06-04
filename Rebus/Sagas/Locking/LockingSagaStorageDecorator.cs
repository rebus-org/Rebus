using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Rebus.Exceptions;
using Rebus.Pipeline;

namespace Rebus.Sagas.Locking
{
    /// <summary>
    /// Decorator of <see cref="ISagaStorage"/> that ensures that an appropriate lock is grabbed/release
    /// around creation and updates to saga data instances.
    /// </summary>
    class LockingSagaStorageDecorator : ISagaStorage
    {
        readonly ISagaStorage _sagaStorage;
        readonly IPessimisticLocker _pessimisticLock;
        readonly TimeSpan _acquireLockTimeout;

        public LockingSagaStorageDecorator(ISagaStorage sagaStorage, IPessimisticLocker pessimisticLock, TimeSpan acquireLockTimeout)
        {
            if (acquireLockTimeout <= TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException(nameof(acquireLockTimeout), $"Cannot use timeout of {acquireLockTimeout} - only values above zero are accepted");
            }
            _sagaStorage = sagaStorage;
            _pessimisticLock = pessimisticLock;
            _acquireLockTimeout = acquireLockTimeout;
        }

        public async Task<ISagaData> Find(Type sagaDataType, string propertyName, object propertyValue)
        {
            var sagaData = await _sagaStorage.Find(sagaDataType, propertyName, propertyValue);

            // if the saga data could not be found, it might be created now... but first we need to be
            // sure that we have exclusive right to create a saga data with the given correlation
            // property, which is why we grab a lock on that
            if (sagaData == null)
            {
                var lockId = $"saga-correlation-{sagaDataType.FullName}-{propertyName}-{propertyValue}";

                // if we got this particular lock, we return null and let it be created
                if (await _pessimisticLock.TryAcquire(lockId))
                {
                    ScheduleLockReleaseWhenMessageContextEnds(lockId);

                    // return null, maybe causing the saga data to be created
                    return null;
                }

                // if we didn't get the lock, someone else may be creating the saga right now.... wait for it to pop up
                // or at least wait and see if we can grab the lock
                while (true)
                {
                    sagaData = await _sagaStorage.Find(sagaDataType, propertyName, propertyValue);

                    if (sagaData != null)
                    {
                        // there was a saga! now we just need to grab the lock (no matter how long it takes) and
                        // then reload the saga data in its current version
                        await AcquireLockPossiblyWait(sagaData.Id.ToString());
                        return await _sagaStorage.Find(sagaDataType, "Id", sagaData.Id);
                    }

                    await Task.Delay(500);
                }
            }

            var sagaDataId = sagaData.Id.ToString();

            // the saga data was there - grab the lock no matter how long it takes
            await AcquireLockPossiblyWait(sagaDataId);
            sagaData = await _sagaStorage.Find(sagaDataType, "Id", sagaDataId);

            // there is a slim possibility that the saga data has been deleted at this point 
            // - therefore:
            if (sagaData == null)
            {
                ScheduleLockReleaseWhenMessageContextEnds(sagaDataId);
            }

            return sagaData;
        }

        void ScheduleLockReleaseWhenMessageContextEnds(string lockId)
        {
            var messageContext = MessageContext.Current;
            if (messageContext == null)
            {
                throw new InvalidOperationException("Could not get current message context - was the Find method called outside of a message handler?");
            }

            // be sure that the lock is released
            messageContext.TransactionContext.OnDisposed(() =>
            {
                Task.Run(async () => await _pessimisticLock.Release(lockId));
            });
        }

        public async Task Insert(ISagaData sagaData, IEnumerable<ISagaCorrelationProperty> correlationProperties)
        {
            // all of the locking for an insert happens by locking on
            // the desired correlation property - and the saga data's ID is new at this point,
            // so there's no need to lock on it

            await _sagaStorage.Insert(sagaData, correlationProperties);
        }

        public async Task Update(ISagaData sagaData, IEnumerable<ISagaCorrelationProperty> correlationProperties)
        {
            try
            {
                await _sagaStorage.Update(sagaData, correlationProperties);
            }
            finally
            {
                var sagaDataId = sagaData.Id.ToString();

                ScheduleLockReleaseWhenMessageContextEnds(sagaDataId);
            }
        }

        public async Task Delete(ISagaData sagaData)
        {
            try
            {
                await _sagaStorage.Delete(sagaData);
            }
            finally
            {
                var sagaDataId = sagaData.Id.ToString();

                ScheduleLockReleaseWhenMessageContextEnds(sagaDataId);
            }
        }

        async Task AcquireLockPossiblyWait(string lockId)
        {
            var random = new Random(DateTime.Now.GetHashCode());
            var stopwatch = Stopwatch.StartNew();

            while (!await _pessimisticLock.TryAcquire(lockId))
            {
                var millisecondsDelay = random.Next(100) + 100;

                await Task.Delay(millisecondsDelay);

                if (stopwatch.Elapsed < _acquireLockTimeout) continue;

                throw new RebusApplicationException($"Could not acquire lock with ID {lockId} within {_acquireLockTimeout} timeout");
            }
        }
    }
}