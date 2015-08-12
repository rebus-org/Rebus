namespace Rebus
{
    /// <summary>
    /// Represents one single configured correlation between the field from one specified incoming
    /// message type and one particular field of a piece of saga data
    /// </summary>
    public abstract class Correlation
    {
        internal abstract string SagaDataPropertyPath { get; }
        
        /// <summary>
        /// Extracts the relevant field from the given message
        /// </summary>
        internal abstract object FieldFromMessage(object message);
    }
}