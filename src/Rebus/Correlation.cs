namespace Rebus
{
    public abstract class Correlation
    {
        internal abstract string SagaDataPropertyPath { get; }
        public abstract object FieldFromMessage(object message);
    }
}