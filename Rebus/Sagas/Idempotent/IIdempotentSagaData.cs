namespace Rebus.Sagas.Idempotent
{
    public interface IIdempotentSagaData : ISagaData
    {
        IdempotencyData IdempotencyData { get; }
    }
}