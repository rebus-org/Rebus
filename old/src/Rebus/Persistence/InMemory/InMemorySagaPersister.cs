using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Ponder;

namespace Rebus.Persistence.InMemory
{
    /// <summary>
    /// Saga persister that stores saga data in memory. Should probably not be used for anything real, except in scenarios
    /// where you know the sagas are very short-lived and don't have to be durable. Saga instances are cloned each time they
    /// are used, so the persister will emulate the workings of durable saga persisters in a fairly realistic manner when used
    /// concurrently.
    /// </summary>
    public class InMemorySagaPersister : IStoreSagaData, IEnumerable<ISagaData>
    {
        static readonly JsonSerializerSettings Settings = new JsonSerializerSettings {TypeNameHandling = TypeNameHandling.All};

        readonly ConcurrentDictionary<Type, ConcurrentDictionary<Guid, ISagaData>> dataByType = new ConcurrentDictionary<Type, ConcurrentDictionary<Guid, ISagaData>>();
        readonly object dataLock = new object();

        /// <summary>
        /// Inserts the given saga data WITHOUT ANY SAFETY! So you're on your own with regards to enforcing the unique
        /// constraing of saga data properties! Why is that? Because at this point, it's impossible to know which
        /// properties are going to be correlation properties.
        /// </summary>
        internal void AddSagaData(ISagaData sagaData)
        {
            lock (dataLock)
            {
                var data = GetData(sagaData.GetType());
                var sagaDataToSave = Clone(sagaData);
                if (sagaDataToSave.Id == Guid.Empty)
                {
                    sagaDataToSave.Id = Guid.NewGuid();
                }
                data[sagaDataToSave.Id] = sagaDataToSave;
            }
        }

        /// <summary>
        /// Stores the saga data in memory, throwing an <see cref="OptimisticLockingException"/> if the unique constraint
        /// of correlation properties is violated
        /// </summary>
        public virtual void Insert(ISagaData sagaData, string[] sagaDataPropertyPathsToIndex)
        {
            var key = sagaData.Id;

            lock (dataLock)
            {
                var data = GetData(sagaData.GetType());

                if (data.ContainsKey(key))
                {
                    throw new OptimisticLockingException(sagaData);
                }

                AssertNoOtherSagaExistsWithSameCorrelationProperty(sagaData, sagaDataPropertyPathsToIndex, data);

                var sagaDataToSave = Clone(sagaData);
                
                sagaDataToSave.Revision++;
                
                data[key] = sagaDataToSave;
            }
        }

        /// <summary>
        /// Updates the given saga data in memory, throwing an <see cref="OptimisticLockingException"/> if either the unique
        /// constraint of correlation properties is violated, or if the revision number does not correspond to the 
        /// "checked out" revision
        /// </summary>
        public void Update(ISagaData sagaData, string[] sagaDataPropertyPathsToIndex)
        {
            var key = sagaData.Id;

            lock (dataLock)
            {
                var data = GetData(sagaData.GetType());

                if (!data.ContainsKey(key))
                {
                    var innerMessage = string.Format("Attempted to update saga with ID {0}, but it was deleted",
                                                     sagaData.Id);

                    throw new OptimisticLockingException(sagaData, new InvalidOperationException(innerMessage));
                }

                if (data.ContainsKey(key))
                {
                    if (data[key].Revision != sagaData.Revision)
                    {
                        throw new OptimisticLockingException(sagaData);
                    }
                }

                AssertNoOtherSagaExistsWithSameCorrelationProperty(sagaData, sagaDataPropertyPathsToIndex, data);

                var sagaDataToSave = Clone(sagaData);
                
                sagaDataToSave.Revision++;
                
                data[key] = sagaDataToSave;
            }
        }

        void AssertNoOtherSagaExistsWithSameCorrelationProperty(ISagaData sagaData, string[] sagaDataPropertyPathsToIndex, ConcurrentDictionary<Guid, ISagaData> data)
        {
            foreach (var otherSagaData in data.Values)
            {
                if (otherSagaData.Id == sagaData.Id) continue;

                foreach (var path in sagaDataPropertyPathsToIndex)
                {
                    var otherValue = (Reflect.Value(otherSagaData, path) ?? "").ToString();
                    var thisValue = (Reflect.Value(sagaData, path) ?? "").ToString();

                    if (thisValue != otherValue) continue;

                    var innerMessage = string.Format("Cannot add saga data because saga with ID {0} already" +
                                                     " has the value {1} for {2}",
                                                     otherSagaData.Id, otherValue, path);

                    throw new OptimisticLockingException(sagaData, new InvalidOperationException(innerMessage));
                }
            }
        }

        /// <summary>
        /// Deletes the given saga data, throwing an <see cref="OptimisticLockingException"/> if it is not present
        /// </summary>
        public void Delete(ISagaData sagaData)
        {
            var key = sagaData.Id;

            lock (dataLock)
            {
                var data = GetData(sagaData.GetType());

                if (!data.ContainsKey(key))
                {
                    throw new OptimisticLockingException(sagaData);
                }

                if (data[key].Revision != sagaData.Revision)
                {
                    var innerMessage = string.Format("Cannot delete saga with ID {0} because it was updated behind our back!",
                                      sagaData.Id);
                    throw new OptimisticLockingException(sagaData, new InvalidOperationException(innerMessage));
                }

                ISagaData removedSagaData;
                data.TryRemove(key, out removedSagaData);
            }
        }

        /// <summary>
        /// Looks up an existing saga data instance by looking at the data property at the path specified by
        /// <paramref name="sagaDataPropertyPath"/> with a value that corresponds to the value specified by <paramref name="fieldFromMessage"/>.
        /// Note that ToString is called on both, so is assumed that the property pointed to by <paramref name="sagaDataPropertyPath"/>
        /// and <paramref name="fieldFromMessage"/> both have a valid string representation
        /// </summary>
        public virtual T Find<T>(string sagaDataPropertyPath, object fieldFromMessage) where T : class, ISagaData
        {
            lock (dataLock)
            {
                var data = GetData(typeof (T));

                return (from sagaData in data
                        let valueFromSagaData = (Reflect.Value(sagaData.Value, sagaDataPropertyPath) ?? "").ToString()
                        where valueFromSagaData.Equals((fieldFromMessage ?? "").ToString())
                        select (T)Clone(sagaData.Value)).FirstOrDefault();
            }
        }

        /// <summary>
        /// Gets an enumerator, allowing the underlying saga data instances to be unumerated
        /// </summary>
        public IEnumerator<ISagaData> GetEnumerator()
        {
            return dataByType.Values.SelectMany(v => v.Values).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        static ISagaData Clone(ISagaData sagaData)
        {
            var jsonObject = JsonConvert.SerializeObject(sagaData, Formatting.Indented, Settings);
            
            return (ISagaData) JsonConvert.DeserializeObject(jsonObject, sagaData.GetType(), Settings);
        }

        ConcurrentDictionary<Guid, ISagaData> GetData(Type type)
        {
            ConcurrentDictionary<Guid, ISagaData> dictionary;
            if (!dataByType.TryGetValue(type, out dictionary))
            {
                dictionary = new ConcurrentDictionary<Guid, ISagaData>();
                dataByType[type] = dictionary;
            }
            return dictionary;
        }
    }
}