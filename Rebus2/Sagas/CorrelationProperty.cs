using System;

namespace Rebus2.Sagas
{
    /// <summary>
    /// Represents a mapping from a field of an incoming message of a specific type to a specific property on a specific type of saga data
    /// </summary>
    public class CorrelationProperty
    {
        public CorrelationProperty(Type messageType, Func<object, object> valueFromMessage, Type sagaDataType, string propertyName)
        {
            PropertyName = propertyName;
            ValueFromMessage = valueFromMessage;
            SagaDataType = sagaDataType;
            MessageType = messageType;
        }

        public Type MessageType { get; private set; }
        
        public Func<object, object> ValueFromMessage { get; private set; }
        
        public Type SagaDataType { get; private set; }

        public string PropertyName { get; private set; }
    }
}