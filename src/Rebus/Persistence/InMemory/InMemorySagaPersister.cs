using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Ponder;

namespace Rebus.Persistence.InMemory
{
    public class InMemorySagaPersister : IStoreSagaData, IEnumerable<ISagaData>
    {
        readonly ConcurrentDictionary<Guid, ISagaData> data = new ConcurrentDictionary<Guid, ISagaData>();

        public virtual void Insert(ISagaData sagaData, string[] sagaDataPropertyPathsToIndex)
        {
            var key = sagaData.Id;
            data[key] = sagaData;
        }

        public void Update(ISagaData sagaData, string[] sagaDataPropertyPathsToIndex)
        {
            var key = sagaData.Id;
            data[key] = sagaData;
        }

        public void Delete(ISagaData sagaData)
        {
            ISagaData temp;
            data.TryRemove(sagaData.Id, out temp);
        }

        public virtual T Find<T>(string sagaDataPropertyPath, object fieldFromMessage) where T : ISagaData
        {
            foreach (var sagaData in data)
            {
                var valueFromSagaData = (Reflect.Value(sagaData.Value, sagaDataPropertyPath) ?? "").ToString();
                if (valueFromSagaData.Equals((fieldFromMessage ?? "").ToString()))
                {
                    return (T) sagaData.Value;
                }
            }
            return default(T);
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