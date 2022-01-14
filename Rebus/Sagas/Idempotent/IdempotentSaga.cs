namespace Rebus.Sagas.Idempotent;

/// <summary>
/// Sagas derived from <see cref="IdempotentSaga{TSagaData}"/> are sagas that guarantee idempotency by guarding against
/// handling the same message twice by tracking IDs of handled messages
/// </summary>
public abstract class IdempotentSaga<TSagaData> : Saga<TSagaData> where TSagaData : IIdempotentSagaData, new()
{
}