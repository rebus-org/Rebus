using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Rebus.Bus;
using Rebus.Extensions;
using Rebus.Messages;
using Rebus.Pipeline;
using Rebus.Pipeline.Receive;
using Rebus.Sagas;
using Rebus.Transport;

namespace Rebus.Auditing.Sagas;

[StepDocumentation("Saves a snapshot of each piece of saga data to the selected snapshot storage.")]
class SaveSagaDataSnapshotStep : IIncomingStep
{
    readonly ISagaSnapshotStorage _sagaSnapshotStorage;
    readonly ITransport _transport;
    readonly string _machineName = GetMachineName();

    public SaveSagaDataSnapshotStep(ISagaSnapshotStorage sagaSnapshotStorage, ITransport transport)
    {
        _sagaSnapshotStorage = sagaSnapshotStorage;
        _transport = transport;
    }

    public async Task Process(IncomingStepContext context, Func<Task> next)
    {
        await next();

        var message = context.Load<Message>();
        var handlerInvokers = context.Load<HandlerInvokers>();

        var createdAndUpdatedSagaData = handlerInvokers
            .Where(i => i.HasSaga)
            .Select(i => new
            {
                i.Handler,
                SagaData = i.GetSagaData()
            })
            .Where(a => a.SagaData != null)
            .ToList();

        var saveTasks = createdAndUpdatedSagaData
            .Select(sagaData =>
            {
                var metadata = GetMetadata(sagaData.SagaData, sagaData.Handler, message);

                return _sagaSnapshotStorage.Save(sagaData.SagaData, metadata);
            });

        await Task.WhenAll(saveTasks);
    }

    Dictionary<string, string> GetMetadata(ISagaData sagaData, object handler, Message message)
    {
        return new Dictionary<string, string>
        {
            {SagaAuditingMetadataKeys.HandleQueue, _transport.Address},
            {SagaAuditingMetadataKeys.SagaDataType, sagaData.GetType().GetSimpleAssemblyQualifiedName()},
            {SagaAuditingMetadataKeys.SagaHandlerType, handler.GetType().GetSimpleAssemblyQualifiedName()},
            {SagaAuditingMetadataKeys.MessageType, message.GetMessageType()},
            {SagaAuditingMetadataKeys.MessageId, message.GetMessageId()},
            {SagaAuditingMetadataKeys.MachineName, _machineName}
        };
    }

    static string GetMachineName() => Environment.MachineName;
}