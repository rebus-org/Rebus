namespace Rebus
{
    public abstract class Correlation
    {
        internal abstract string SagaDataPropertyPath { get; }
        internal abstract string MessagePropertyPath { get; }

        public abstract object FieldFromMessage(object message);
    }
}