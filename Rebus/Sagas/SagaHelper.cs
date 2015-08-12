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
        /// Gets (most likely from a cache) the set of correlation properties relevant for the given saga handler.
        /// </summary>
        public SagaDataCorrelationProperties GetCorrelationProperties(object message, Saga saga)
        {
            var sagaDataType = saga.GetSagaDataType();

            var correlationPropertiesForThisSagaDataType = _cachedCorrelationProperties
                .GetOrAdd(sagaDataType, type => GetCorrelationProperties(saga));

            return new SagaDataCorrelationProperties(correlationPropertiesForThisSagaDataType, sagaDataType);
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