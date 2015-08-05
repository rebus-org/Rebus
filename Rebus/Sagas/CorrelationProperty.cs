using System;
using System.Linq;

namespace Rebus.Sagas
{
    /// <summary>
    /// Represents a mapping from a field of an incoming message of a specific type to a specific property on a specific type of saga data
    /// </summary>
    public class CorrelationProperty : ISagaCorrelationProperty
    {
        /// <summary>
        /// Defines the types that are allowed to use with saga data properties that are intended for correlation
        /// </summary>
        static readonly Type[] AllowedCorrelationPropertyTypes =
        {
            typeof (Guid),
            typeof (int),
            typeof (string),
        };

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
            if (messageType == null) throw new ArgumentNullException("messageType");
            if (sagaDataType == null) throw new ArgumentNullException("sagaDataType");
            if (propertyName == null) throw new ArgumentNullException("propertyName");
            if (sagaType == null) throw new ArgumentNullException("sagaType");
            PropertyName = propertyName;
            SagaType = sagaType;
            ValueFromMessage = valueFromMessage;
            SagaDataType = sagaDataType;
            MessageType = messageType;

            Validate();
        }

        void Validate()
        {
            var propertyType = SagaDataType.GetProperty(PropertyName).PropertyType;

            if (AllowedCorrelationPropertyTypes.Contains(propertyType)) return;

            throw new ArgumentException(string.Format("Cannot correlate with the '{0}' property on the '{1}' saga data type - only allowed types are: {2}",
                PropertyName, SagaDataType.Name, string.Join(", ", AllowedCorrelationPropertyTypes.Select(t => t.Name))));
        }

        /// <summary>
        /// The message type that this property can correlate
        /// </summary>
        public Type MessageType { get; private set; }
        
        /// <summary>
        /// The function that will be called with the message instance in order to extract a value that should be used for correlation
        /// </summary>
        public Func<object, object> ValueFromMessage { get; private set; }
        
        /// <summary>
        /// Gets the type of the saga's saga data
        /// </summary>
        public Type SagaDataType { get; private set; }

        /// <summary>
        /// Gets the name of the correlation property
        /// </summary>
        public string PropertyName { get; private set; }
        
        /// <summary>
        /// The saga type (i.e. the handler type) that contains the logic of the saga
        /// </summary>
        public Type SagaType { get; private set; }
    }
}