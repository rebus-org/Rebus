using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace Rebus.Sagas
{
    /// <summary>
    /// Helper class that can cache configured sets of correlation properties for various saga types
    /// </summary>
    public class SagaHelper
    {
        readonly ConcurrentDictionary<Type, Dictionary<Type, CorrelationProperty[]>> _cachedCorrelationProperties
                = new ConcurrentDictionary<Type, Dictionary<Type, CorrelationProperty[]>>();

        /// <summary>
        /// Gets (possibly from the cache) the set of correlation properties relevant for the given message type and saga handler.
        /// If no properties are found, an <see cref="ArgumentException"/> is thrown.
        /// </summary>
        /// <exception cref="ArgumentException"></exception>
        public IEnumerable<CorrelationProperty> GetCorrelationProperties(object message, Saga saga)
        {
            var sagaDataType = saga.GetSagaDataType();
            var messageType = message.GetType();

            var correlationPropertiesForThisSagaDataType = _cachedCorrelationProperties
                .GetOrAdd(sagaDataType, type => GetCorrelationProperties(saga));

            CorrelationProperty[] potentialCorrelationproperties;
            
            if (!correlationPropertiesForThisSagaDataType.TryGetValue(messageType, out potentialCorrelationproperties))
            {
                throw new ArgumentException(string.Format("Could not find any correlation properties for message {0} and saga data {1}", messageType, sagaDataType));
            }

            return potentialCorrelationproperties;
        }

        /// <summary>
        /// Creates a new instance of the saga's saga data
        /// </summary>
        public ISagaData CreateNewSagaData(Saga saga)
        {
            return saga.CreateNewSagaData();
        }

        static Dictionary<Type, CorrelationProperty[]> GetCorrelationProperties(Saga saga)
        {
            return saga.GenerateCorrelationProperties()
                .ToLookup(p => p.MessageType)
                .ToDictionary(kvp => kvp.Key, kvp => kvp.ToArray());
        }
    }
}