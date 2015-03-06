using System;

namespace Rebus.Sagas
{
    /// <summary>
    /// Represents a path to a correlation property on a saga of a specific type
    /// </summary>
    public interface ISagaCorrelationProperty
    {
        string PropertyName { get; }
        Type SagaDataType { get; }
    }

    /// <summary>
    /// Represents a mapping from a field of an incoming message of a specific type to a specific property on a specific type of saga data
    /// </summary>
    public class CorrelationProperty : ISagaCorrelationProperty
    {
        public CorrelationProperty(Type messageType, Func<object, object> valueFromMessage, Type sagaDataType, string propertyName, Type sagaType)
        {
            PropertyName = propertyName;
            SagaType = sagaType;
            ValueFromMessage = valueFromMessage;
            SagaDataType = sagaDataType;
            MessageType = messageType;
        }

        public Type MessageType { get; private set; }
        
        public Func<object, object> ValueFromMessage { get; private set; }
        
        public Type SagaDataType { get; private set; }

        public string PropertyName { get; private set; }
        
        public Type SagaType { get; private set; }
    }
}