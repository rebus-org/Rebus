using System;
using System.Linq;
using System.Reflection;

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
            typeof (bool),
            typeof (byte),
            typeof (short),
            typeof (int),
            typeof (long),
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
            if (messageType == null) throw new ArgumentNullException(nameof(messageType));
            if (sagaDataType == null) throw new ArgumentNullException(nameof(sagaDataType));
            if (propertyName == null) throw new ArgumentNullException(nameof(propertyName));
            if (sagaType == null) throw new ArgumentNullException(nameof(sagaType));
            PropertyName = propertyName;
            SagaType = sagaType;
            ValueFromMessage = valueFromMessage;
            SagaDataType = sagaDataType;
            MessageType = messageType;
            Validate();
        }

        void Validate()
        {
            if (string.IsNullOrWhiteSpace(PropertyName))
            {
                throw new ArgumentException($"Reflected saga data correlation property name from {SagaDataType} is empty! This is most likely because the expression passed to the correlation configuration could not be properly reflected - it's the part indicated by !!! here: config.Correlate<TMessage>(m => m.SomeField, d => !!!) - please be sure that you are pointing to a simple property of the saga data");
            }

            var sagaDataProperty = SagaDataType.GetTypeInfo().GetProperty(PropertyName);

            if (sagaDataProperty == null)
            {
                throw new ArgumentException($"Could not find correlation property '{PropertyName}' on saga data of type {SagaDataType}!");
            }

            var propertyType = sagaDataProperty.PropertyType;

            if (AllowedCorrelationPropertyTypes.Contains(propertyType)) return;

            var allowedTypes = string.Join(", ", AllowedCorrelationPropertyTypes.Select(t => t.Name));

            throw new ArgumentException($"Cannot correlate with the '{PropertyName}' property on the '{SagaDataType.Name}' saga data type - only allowed types are: {allowedTypes}");
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
        public Type SagaDataType { get; }

        /// <summary>
        /// Gets the name of the correlation property
        /// </summary>
        public string PropertyName { get; }
        
        /// <summary>
        /// The saga type (i.e. the handler type) that contains the logic of the saga
        /// </summary>
        public Type SagaType { get; }
    }
}