using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Rebus.Pipeline.Receive;

namespace Rebus.Sagas
{
    public  class SagaHelper
    {
        readonly ConcurrentDictionary<Type, Dictionary<Type, CorrelationProperty[]>> _cachedCorrelationProperties
                = new ConcurrentDictionary<Type, Dictionary<Type, CorrelationProperty[]>>();

        public IEnumerable<CorrelationProperty> GetCorrelationProperties(HandlerInvoker sagaInvoker, object message)
        {
            var saga = sagaInvoker.Saga;
            var sagaDataType = saga.GetSagaDataType();
            var messageType = message.GetType();

            var correlationPropertiesForThisSagaDataType = _cachedCorrelationProperties
                .GetOrAdd(sagaDataType, type => GetCorrelationProperties(saga));

            CorrelationProperty[] potentialCorrelationproperties;
            if (!correlationPropertiesForThisSagaDataType.TryGetValue(messageType, out potentialCorrelationproperties))
            {
                throw new ArgumentException(string.Format("Could not find any correlation properties for message {0}", messageType));
            }

            return potentialCorrelationproperties;
        }

        Dictionary<Type, CorrelationProperty[]> GetCorrelationProperties(Saga saga)
        {
            return saga.GenerateCorrelationProperties()
                .ToLookup(p => p.MessageType)
                .ToDictionary(kvp => kvp.Key, kvp => kvp.ToArray());
        }

        public ISagaData CreateNewSagaData(Saga saga)
        {
            return saga.CreateNewSagaData();
        }
    }
}