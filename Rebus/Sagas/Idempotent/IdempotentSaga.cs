namespace Rebus.Sagas.Idempotent
{
    public abstract class IdempotentSaga<TSagaData> : Saga<TSagaData> where TSagaData : IIdempotentSagaData, new()
    {
    }
}