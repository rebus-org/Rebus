using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Rebus.Exceptions;
using Rebus.Reflection;
using Rebus.Sagas;

namespace Rebus.Persistence.InMem
{
    public class InMemorySagaStorage : ISagaStorage
    {
        readonly ConcurrentDictionary<Guid, ISagaData> _data = new ConcurrentDictionary<Guid, ISagaData>();
        readonly object _lock = new object();

        readonly JsonSerializerSettings _serializerSettings = new JsonSerializerSettings
        {
            TypeNameHandling = TypeNameHandling.All
        };

        public async Task<ISagaData> Find(Type sagaDataType, string propertyName, object propertyValue)
        {
            lock (_lock)
            {
                var valueFromMessage = (propertyValue ?? "").ToString();

                foreach (var data in _data.Values)
                {
                    if (data.GetType() != sagaDataType) continue;

                    var valueFromSaga = (Reflect.Value(data, propertyName) ?? "").ToString();

                    if (valueFromMessage.Equals(valueFromSaga)) return Clone(data);
                }

                return null;
            }
        }

        public async Task Insert(ISagaData sagaData)
        {
            var id = GetId(sagaData);

            lock (_lock)
            {
                if (_data.ContainsKey(id))
                {
                    throw new ConcurrencyException("Saga data with ID {0} already exists!", id);
                }

                if (sagaData.Revision != 0)
                {
                    throw new InvalidOperationException(string.Format("Attempted to insert saga data with ID {0} and revision {1}, but revision must be 0 on first insert!",
                        id, sagaData.Revision));
                }

                _data[id] = Clone(sagaData);
            }
        }

        public async Task Update(ISagaData sagaData)
        {
            var id = GetId(sagaData);

            lock (_lock)
            {
                if (!_data.ContainsKey(id))
                {
                    throw new ConcurrencyException("Saga data with ID {0} no longer exists and cannot be updated", id);
                }

                var existingCopy = _data[id];

                if (existingCopy.Revision != sagaData.Revision)
                {
                    throw new ConcurrencyException("Attempted to update saga data with ID {0} with revision {1}, but the existing data was updated to revision {2}",
                        id, sagaData.Revision, existingCopy.Revision);
                }

                var clone = Clone(sagaData);
                clone.Revision++;
                _data[id] = clone;
            }
        }

        public async Task Delete(ISagaData sagaData)
        {
            var id = GetId(sagaData);

            lock (_lock)
            {
                if (!_data.ContainsKey(id))
                {
                    throw new ConcurrencyException("Saga data with ID {0} no longer exists and cannot be deleted", id);
                }

                ISagaData temp;
                _data.TryRemove(id, out temp);
            }
        }

        static Guid GetId(ISagaData sagaData)
        {
            var id = sagaData.Id;
            
            if (id != Guid.Empty) return id;

            throw new InvalidOperationException("Saga data must be provided with an ID in order to do this!");
        }

        ISagaData Clone(ISagaData sagaData)
        {
            var serializedObject = JsonConvert.SerializeObject(sagaData, _serializerSettings);
            return JsonConvert.DeserializeObject<ISagaData>(serializedObject, _serializerSettings);
        }
    }
}