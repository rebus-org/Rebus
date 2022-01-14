namespace Rebus.Sagas.Idempotent;

/// <summary>
/// Convenient standard implementation of <see cref="IIdempotentSagaData"/>
/// </summary>
public abstract class IdempotentSagaData : SagaData, IIdempotentSagaData
{
    /// <summary>
    /// The idempotency data stores the side-effects
    /// </summary>
    public IdempotencyData IdempotencyData { get; set; }
}