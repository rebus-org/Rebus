namespace Rebus
{
    public abstract class Correlation
    {
        internal abstract string SagaDataPropertyPath { get; }
        internal abstract string MessagePropertyPath { get; }

        public abstract string FieldFromMessage<TMessage>(TMessage message);
    }
}