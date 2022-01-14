namespace Rebus.Sagas.Idempotent;

/// <summary>
/// Derivation of <see cref="ISagaData"/> that is capable of storing externally visible side-effects (i.e. outgoing messages)
/// that were caused by handling specific incoming messages
/// </summary>
public interface IIdempotentSagaData : ISagaData
{
    /// <summary>
    /// The idempotency data stores the side-effects
    /// </summary>
    IdempotencyData IdempotencyData { get; set; }
}