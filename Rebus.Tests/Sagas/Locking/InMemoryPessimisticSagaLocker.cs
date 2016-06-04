using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Rebus.Pipeline;
using Rebus.Sagas;

namespace Rebus.Tests.Sagas.Locking
{
    class InMemoryPessimisticSagaLocker : ISagaStorage
    {
        readonly ISagaStorage _sagaStorage;
        readonly InMemorySagaLocks _inMemorySagaLocks;

        public InMemoryPessimisticSagaLocker(ISagaStorage sagaStorage, InMemorySagaLocks inMemorySagaLocks)
        {
            _sagaStorage = sagaStorage;
            _inMemorySagaLocks = inMemorySagaLocks;
        }

        public async Task<ISagaData> Find(Type sagaDataType, string propertyName, object propertyValue)
        {
            var sagaData = await _sagaStorage.Find(sagaDataType, propertyName, propertyValue);

            if (sagaData == null)
            {
                var lockId = $"{sagaDataType.FullName}-{propertyName}-{propertyValue}";

                // if we got this particular lock, we return null and let it be created
                if (await _inMemorySagaLocks.TryGrab(lockId))
                {
                    var messageContext = MessageContext.Current;
                    if (messageContext == null)
                    {
                        throw new InvalidOperationException("Could not get current message context - was the Find method called outside of a message handler?");
                    }

                    messageContext.TransactionContext.OnDisposed(() =>
                    {
                        Task.Run(async () => await _inMemorySagaLocks.Release(lockId));
                    });

                    return null;
                }

                // if we didn't get the lock, someone else is creating the saga right now.... wait for it!
                while (true)
                {
                    sagaData = await _sagaStorage.Find(sagaDataType, propertyName, propertyValue);

                    if (sagaData != null)
                    {
                        await GrabLock(sagaData.Id.ToString());
                        return await _sagaStorage.Find(sagaDataType, "Id", sagaData.Id);
                    }

                    await Task.Delay(500);
                }
            }

            await GrabLock(sagaData.Id.ToString());
            return await _sagaStorage.Find(sagaDataType, "Id", sagaData.Id);
        }

        public async Task Insert(ISagaData sagaData, IEnumerable<ISagaCorrelationProperty> correlationProperties)
        {
            await GrabLock(sagaData.Id.ToString());

            try
            {
                await _sagaStorage.Insert(sagaData, correlationProperties);
            }
            finally
            {
                await ReleaseLock(sagaData.Id.ToString());
            }
        }

        public async Task Update(ISagaData sagaData, IEnumerable<ISagaCorrelationProperty> correlationProperties)
        {
            try
            {
                await _sagaStorage.Update(sagaData, correlationProperties);
            }
            finally
            {
                await ReleaseLock(sagaData.Id.ToString());
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
                await ReleaseLock(sagaData.Id.ToString());
            }
        }

        async Task GrabLock(string lockId)
        {
            var random = new Random(DateTime.Now.GetHashCode());

            while (!await _inMemorySagaLocks.TryGrab(lockId))
            {
                var millisecondsDelay = random.Next(200) + 200;
                await Task.Delay(millisecondsDelay);
            }
        }

        async Task ReleaseLock(string lockId)
        {
            await _inMemorySagaLocks.Release(lockId);
        }
    }
}