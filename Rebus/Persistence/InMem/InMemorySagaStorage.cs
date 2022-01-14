using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Rebus.Exceptions;
using Rebus.Reflection;
using Rebus.Sagas;
#pragma warning disable 1998

namespace Rebus.Persistence.InMem;

/// <summary>
/// Implementation of <see cref="ISagaStorage"/> that "persists" saga data in memory. Saga data is serialized/deserialized using Newtonsoft JSON.NET
/// with some pretty robust settings, so inheritance and interfaces etc. can be used in the saga data.
/// </summary>
public class InMemorySagaStorage : ISagaStorage
{
    readonly ConcurrentDictionary<Guid, ISagaData> _data = new ConcurrentDictionary<Guid, ISagaData>();
    readonly object _lock = new object();

    readonly JsonSerializerSettings _serializerSettings = new JsonSerializerSettings
    {
        TypeNameHandling = TypeNameHandling.All
    };

    /// <summary>
    /// Returns all stored saga data instances. 
    /// </summary>
    public IEnumerable<ISagaData> Instances
    {
        get
        {
            lock (_data)
            {
                return _data.Values.ToList();
            }
        }
    }

    internal void AddInstance(ISagaData sagaData)
    {
        lock (_lock)
        {
            var instance = Clone(sagaData);
            if (instance.Id == Guid.Empty)
            {
                instance.Id = Guid.NewGuid();
            }
            _data[instance.Id] = instance;
        }
    }

    internal event Action<ISagaData> Created;
    internal event Action<ISagaData> Updated;
    internal event Action<ISagaData> Deleted;
    internal event Action<ISagaData> Correlated;
    internal event Action CouldNotCorrelate;

    /// <summary>
    /// Looks up an existing saga data of the given type with a property of the specified name and the specified value
    /// </summary>
    public async Task<ISagaData> Find(Type sagaDataType, string propertyName, object propertyValue)
    {
        lock (_lock)
        {
            var valueFromMessage = (propertyValue ?? "").ToString();

            foreach (var data in _data.Values)
            {
                if (data.GetType() != sagaDataType) continue;

                var sagaValue = Reflect.Value(data, propertyName);
                var valueFromSaga = (sagaValue ?? "").ToString();

                if (valueFromMessage.Equals(valueFromSaga))
                {
                    var clone = Clone(data);
                    Correlated?.Invoke(clone);
                    return clone;
                }
            }

            CouldNotCorrelate?.Invoke();
            return null;
        }
    }

    /// <summary>
    /// Saves the given saga data, throwing an exception if the instance already exists
    /// </summary>
    public async Task Insert(ISagaData sagaData, IEnumerable<ISagaCorrelationProperty> correlationProperties)
    {
        var id = GetId(sagaData);

        lock (_lock)
        {
            if (_data.ContainsKey(id))
            {
                throw new ConcurrencyException($"Saga data with ID {id} already exists!");
            }

            VerifyCorrelationPropertyUniqueness(sagaData, correlationProperties);

            if (sagaData.Revision != 0)
            {
                throw new InvalidOperationException($"Attempted to insert saga data with ID {id} and revision {sagaData.Revision}, but revision must be 0 on first insert!");
            }

            var clone = Clone(sagaData);
            _data[id] = clone;
            Created?.Invoke(clone);
        }
    }

    /// <summary>
    /// Updates the saga data
    /// </summary>
    public async Task Update(ISagaData sagaData, IEnumerable<ISagaCorrelationProperty> correlationProperties)
    {
        var id = GetId(sagaData);

        lock (_lock)
        {
            if (!_data.ContainsKey(id))
            {
                throw new ConcurrencyException($"Saga data with ID {id} no longer exists and cannot be updated");
            }

            VerifyCorrelationPropertyUniqueness(sagaData, correlationProperties);

            var existingCopy = _data[id];

            if (existingCopy.Revision != sagaData.Revision)
            {
                throw new ConcurrencyException($"Attempted to update saga data with ID {id} with revision {sagaData.Revision}, but the existing data was updated to revision {existingCopy.Revision}");
            }

            var clone = Clone(sagaData);
            clone.Revision++;
            _data[id] = clone;
            Updated?.Invoke(clone);
            sagaData.Revision++;
        }
    }

    void VerifyCorrelationPropertyUniqueness(ISagaData sagaData, IEnumerable<ISagaCorrelationProperty> correlationProperties)
    {
        foreach (var property in correlationProperties)
        {
            var valueFromSagaData = Reflect.Value(sagaData, property.PropertyName);

            foreach (var existingSagaData in _data.Values)
            {
                if (existingSagaData.Id == sagaData.Id) continue;
                if (existingSagaData.GetType() != sagaData.GetType()) continue;

                var valueFromExistingInstance = Reflect.Value(existingSagaData, property.PropertyName);

                if (Equals(valueFromSagaData, valueFromExistingInstance))
                {
                    throw new ConcurrencyException($"Correlation property '{property.PropertyName}' has value '{valueFromExistingInstance}' in existing saga data with ID {existingSagaData.Id}");
                }
            }
        }
    }

    /// <summary>
    /// Deletes the given saga data
    /// </summary>
    public async Task Delete(ISagaData sagaData)
    {
        var id = GetId(sagaData);

        lock (_lock)
        {
            if (!_data.ContainsKey(id))
            {
                throw new ConcurrencyException($"Saga data with ID {id} no longer exists and cannot be deleted");
            }

            ISagaData temp;
            if (_data.TryRemove(id, out temp))
            {
                Deleted?.Invoke(temp);
            }
            sagaData.Revision++;
        }
    }

    /// <summary>
    /// Resets the saga storage (i.e. all stored saga data instances are deleted)
    /// </summary>
    public void Reset()
    {
        lock (_data)
        {
            foreach (var id in _data.Keys.ToList())
            {
                if (_data.TryRemove(id, out var sagaData))
                {
                    Deleted?.Invoke(sagaData);
                }
            }
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