using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Ponder;

namespace Rebus.Persistence.InMemory
{
    /// <summary>
    /// Saga persister that stores saga data in memory. Should probably not be used for anything real, except in scenarios
    /// where you know the sagas are very short-lived and don't have to be durable.
    /// </summary>
    public class InMemorySagaPersister : IStoreSagaData, IEnumerable<ISagaData>
    {
        readonly ConcurrentDictionary<Guid, ISagaData> data = new ConcurrentDictionary<Guid, ISagaData>();

        /// <summary>
        /// Constructs the persister
        /// </summary>
        public InMemorySagaPersister()
        {
        }

        internal InMemorySagaPersister(IEnumerable<ISagaData> sagaDataBootstrap)
        {
            foreach (var sagaData in sagaDataBootstrap)
            {
                if (sagaData.Id == Guid.Empty)
                    sagaData.Id = Guid.NewGuid();

                data[sagaData.Id] = sagaData;
            }
        }

        public virtual void Insert(ISagaData sagaData, string[] sagaDataPropertyPathsToIndex)
        {
            var key = sagaData.Id;

            if (data.ContainsKey(key))
            {
                if (data[key].Revision != sagaData.Revision)
                {
                    throw new OptimisticLockingException(sagaData);
                }
            }

            sagaData.Revision++;
            
            data[key] = sagaData;
        }

        public void Update(ISagaData sagaData, string[] sagaDataPropertyPathsToIndex)
        {
            var key = sagaData.Id;

            if (data.ContainsKey(key))
            {
                if (data[key].Revision != sagaData.Revision)
                {
                    throw new OptimisticLockingException(sagaData);
                }
            }

            sagaData.Revision++;

            data[key] = sagaData;
        }

        public void Delete(ISagaData sagaData)
        {
            ISagaData temp;

            var key = sagaData.Id;
            
            if (!data.ContainsKey(key))
            {
                throw new OptimisticLockingException(sagaData);
            }
            
            data.TryRemove(key, out temp);
        }

        public virtual T Find<T>(string sagaDataPropertyPath, object fieldFromMessage) where T : class, ISagaData
        {
            return (from sagaData in data
                    let valueFromSagaData = (Reflect.Value(sagaData.Value, sagaDataPropertyPath) ?? "").ToString()
                    where valueFromSagaData.Equals((fieldFromMessage ?? "").ToString())
                    select (T) sagaData.Value).FirstOrDefault();
        }

        public IEnumerator<ISagaData> GetEnumerator()
        {
            return data.Values.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}