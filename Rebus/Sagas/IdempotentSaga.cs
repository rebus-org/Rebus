namespace Rebus.Sagas
{
    public abstract class IdempotentSaga<TSagaData> : Saga<TSagaData> where TSagaData : ISagaData, new()
    {
    }
}