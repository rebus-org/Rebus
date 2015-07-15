namespace Rebus.Sagas.Idempotent
{
    /// <summary>
    /// Convenient standard implementation of <see cref="IIdempotentSagaData"/>
    /// </summary>
    public abstract class IdempotentSagaData : SagaData, IIdempotentSagaData
    {
        public IdempotencyData IdempotencyData { get; set; }
    }
}