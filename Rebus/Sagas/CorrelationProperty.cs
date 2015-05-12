using System;

namespace Rebus.Sagas
{
    /// <summary>
    /// Represents a mapping from a field of an incoming message of a specific type to a specific property on a specific type of saga data
    /// </summary>
    public class CorrelationProperty : ISagaCorrelationProperty
    {
        /// <summary>
        /// Constructs the correlation property
        /// </summary>
        /// <param name="messageType">Specifies the message type that this property can correlate</param>
        /// <param name="valueFromMessage">Specifies the function that will be called with the message instance in order to extract a value that should be used for correlation</param>
        /// <param name="sagaDataType">Specifies the type of saga data that this property can correlate to</param>
        /// <param name="propertyName">Specifies that property name on the saga data that this correlation addresses</param>
        /// <param name="sagaType">Specifies the saga type (i.e. the handler type) that contains the logic of the saga</param>
        public CorrelationProperty(Type messageType, Func<object, object> valueFromMessage, Type sagaDataType, string propertyName, Type sagaType)
        {
            PropertyName = propertyName;
            SagaType = sagaType;
            ValueFromMessage = valueFromMessage;
            SagaDataType = sagaDataType;
            MessageType = messageType;
        }

        /// <summary>
        /// The message type that this property can correlate
        /// </summary>
        public Type MessageType { get; private set; }
        
        /// <summary>
        /// The function that will be called with the message instance in order to extract a value that should be used for correlation
        /// </summary>
        public Func<object, object> ValueFromMessage { get; private set; }
        
        public Type SagaDataType { get; private set; }

        public string PropertyName { get; private set; }
        
        /// <summary>
        /// The saga type (i.e. the handler type) that contains the logic of the saga
        /// </summary>
        public Type SagaType { get; private set; }
    }
}