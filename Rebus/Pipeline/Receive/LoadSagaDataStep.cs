using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Rebus.Extensions;
using Rebus.Logging;
using Rebus.Messages;
using Rebus.Sagas;

namespace Rebus.Pipeline.Receive
{
    /// <summary>
    /// Incoming step that loads and saves relevant saga data.
    /// </summary>
    public class LoadSagaDataStep : IIncomingStep
    {
        static ILog _log;

        static LoadSagaDataStep()
        {
            RebusLoggerFactory.Changed += f => _log = f.GetCurrentClassLogger();
        }

        readonly SagaHelper _sagaHelper = new SagaHelper();
        readonly ISagaStorage _sagaStorage;

        /// <summary>
        /// Constructs the step with the given saga storage
        /// </summary>
        public LoadSagaDataStep(ISagaStorage sagaStorage)
        {
            _sagaStorage = sagaStorage;
        }

        public async Task Process(IncomingStepContext context, Func<Task> next)
        {
            var handlerInvokersForSagas = context.Load<HandlerInvokers>()
                .Where(l => l.HasSaga)
                .ToList();

            var message = context.Load<Message>();
            var messageId = message.Headers.GetValue(Headers.MessageId);
            var body = message.Body;
            var loadedSagaData = new List<RelevantSagaInfo>();
            var newlyCreatedSagaData = new List<RelevantSagaInfo>();

            foreach (var sagaInvoker in handlerInvokersForSagas)
            {
                var foundExistingSagaData = false;
                
                var correlationProperties = _sagaHelper
                    .GetCorrelationProperties(body, sagaInvoker.Saga)
                    .ToList();

                foreach (var correlationProperty in correlationProperties)
                {
                    var valueFromMessage = correlationProperty.ValueFromMessage(body);
                    var sagaData = await _sagaStorage.Find(sagaInvoker.Saga.GetSagaDataType(), correlationProperty.PropertyName, valueFromMessage);

                    if (sagaData == null) continue;

                    sagaInvoker.SetSagaData(sagaData);
                    foundExistingSagaData = true;
                    loadedSagaData.Add(new RelevantSagaInfo(sagaData, correlationProperties, sagaInvoker.Saga));

                    _log.Debug("Found existing saga data with ID {0} for message {1}", sagaData.Id, messageId);
                    break;
                }

                if (!foundExistingSagaData)
                {
                    var canBeInitiatedByThisMessageType = sagaInvoker.CanBeInitiatedBy(body.GetType());

                    if (canBeInitiatedByThisMessageType)
                    {
                        var newSagaData = _sagaHelper.CreateNewSagaData(sagaInvoker.Saga);
                        sagaInvoker.SetSagaData(newSagaData);
                        _log.Debug("Created new saga data with ID {0} for message {1}", newSagaData.Id, messageId);
                        newlyCreatedSagaData.Add(new RelevantSagaInfo(newSagaData, correlationProperties, sagaInvoker.Saga));
                    }
                    else
                    {
                        _log.Debug("Could not find existing saga data for message {0}", messageId);
                        sagaInvoker.SkipInvocation();
                    }
                }
            }

            await next();

            var newlyCreatedSagaDataToSave = newlyCreatedSagaData.Where(s => !s.Saga.WasMarkedAsComplete);
            var loadedSagaDataToUpdate = loadedSagaData.Where(s => !s.Saga.WasMarkedAsComplete);
            var loadedSagaDataToDelete = loadedSagaData.Where(s => s.Saga.WasMarkedAsComplete);

            foreach (var sagaDataToInsert in newlyCreatedSagaDataToSave)
            {
                await _sagaStorage.Insert(sagaDataToInsert.SagaData, sagaDataToInsert.CorrelationProperties);
            }

            foreach (var sagaDataToUpdate in loadedSagaDataToUpdate)
            {
                await _sagaStorage.Update(sagaDataToUpdate.SagaData, sagaDataToUpdate.CorrelationProperties);
            }

            foreach (var sagaDataToUpdate in loadedSagaDataToDelete)
            {
                await _sagaStorage.Delete(sagaDataToUpdate.SagaData);
            }
        }

        class RelevantSagaInfo
        {
            public RelevantSagaInfo(ISagaData sagaData, List<CorrelationProperty> correlationProperties, Saga saga)
            {
                SagaData = sagaData;
                CorrelationProperties = correlationProperties;
                Saga = saga;
            }

            public ISagaData SagaData { get; private set; }
            
            public List<CorrelationProperty> CorrelationProperties { get; private set; }
            
            public Saga Saga { get; private set; }
        }
    }
}