using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
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