using System;
using System.Threading.Tasks;
using Rebus.Sagas;
using Rebus.Tests.Contracts.Utilities;
#pragma warning disable 1998

namespace Rebus.Tests.Sagas.TestIdCorrelation;

public class DefaultSagaData : SagaData
{
}

public class DefaultSaga : Saga<DefaultSagaData>, IAmInitiatedBy<DefaultSagaMessage>
{
    readonly SharedCounter _counter;

    public DefaultSaga(SharedCounter counter)
    {
        _counter = counter;
    }

    protected override void CorrelateMessages(ICorrelationConfig<DefaultSagaData> config)
    {
        config.Correlate<DefaultSagaMessage>(m => m.SagaId, d => d.Id);
    }

    public async Task Handle(DefaultSagaMessage message)
    {
        _counter.Decrement();
    }
}

public class DefaultSagaMessage
{
    public DefaultSagaMessage(Guid sagaId)
    {
        SagaId = sagaId;
    }

    public Guid SagaId { get; }
}