using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Rebus.Sagas
{
    /// <summary>
    /// Contains a set of correlation properties relevant for one particular saga data
    /// </summary>
    public class SagaDataCorrelationProperties : IEnumerable<CorrelationProperty>
    {
        readonly Dictionary<Type, CorrelationProperty[]> _correlationProperties;
        readonly Type _sagaDataType;

        /// <summary>
        /// Constructs the set
        /// </summary>
        public SagaDataCorrelationProperties(Dictionary<Type, CorrelationProperty[]> correlationProperties, Type sagaDataType)
        {
            if (correlationProperties == null) throw new ArgumentNullException("correlationProperties");
            if (sagaDataType == null) throw new ArgumentNullException("sagaDataType");
            
            _correlationProperties = correlationProperties;
            _sagaDataType = sagaDataType;
        }

        /// <summary>
        /// Looks up correlation properties relevant for the specified message type
        /// </summary>
        public IEnumerable<CorrelationProperty> ForMessage(object body)
        {
            if (body == null) throw new ArgumentNullException("body");

            CorrelationProperty[] potentialCorrelationproperties;
            var messageType = body.GetType();

            if (!_correlationProperties.TryGetValue(messageType, out potentialCorrelationproperties))
            {
                throw new ArgumentException(string.Format("Could not find any correlation properties for message {0} and saga data {1}", 
                    messageType, _sagaDataType));
            }

            return potentialCorrelationproperties;
        }

        /// <summary>
        /// Gets the correlation properties contained in this collection
        /// </summary>
        public IEnumerator<CorrelationProperty> GetEnumerator()
        {
            return _correlationProperties.SelectMany(kvp => kvp.Value).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}