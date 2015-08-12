using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Rebus.Bus;
using Rebus.Logging;
using Rebus.Messages;
using Rebus.Sagas;

namespace Rebus.Pipeline.Receive
{
    /// <summary>
    /// Incoming step that loads and saves relevant saga data.
    /// </summary>
    [StepDocumentation(@"Looks at the handler invokers in the context and sees if there's one or more saga handlers in there. 

If that's the case, relevant saga data is loaded/created, and the rest of the pipeline gets invoked.

Afterwards, all the created/loaded saga data is updated appropriately.")]
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

        /// <summary>
        /// For each <see cref="HandlerInvoker"/> found in the current <see cref="IncomingStepContext"/>'s <see cref="HandlerInvokers"/>,
        /// this step will see if the invoker's handler is actually a <see cref="Saga"/>. If that is the case, the saga's correlation properties
        /// are used to see if a piece of existing saga data can be retrieved and mounted on the <see cref="Saga{TSagaData}.Data"/> property.
        /// If no existing instance was found, but the saga implements <see cref="IAmInitiatedBy{TMessage}"/> for the current message,
        /// a new saga data instance will be created (and mounted). Otherwise, the message is ignored.
        /// </summary>
        public async Task Process(IncomingStepContext context, Func<Task> next)
        {
            var handlerInvokersForSagas = context.Load<HandlerInvokers>()
                .Where(l => l.HasSaga)
                .ToList();

            var message = context.Load<Message>();
            var label = message.GetMessageLabel();

            var body = message.Body;
            var loadedSagaData = new List<RelevantSagaInfo>();
            var newlyCreatedSagaData = new List<RelevantSagaInfo>();

            foreach (var sagaInvoker in handlerInvokersForSagas)
            {
                var foundExistingSagaData = false;

                var correlationProperties = _sagaHelper.GetCorrelationProperties(body, sagaInvoker.Saga);
                var correlationPropertiesRelevantForMessage = correlationProperties.ForMessage(body);

                foreach (var correlationProperty in correlationPropertiesRelevantForMessage)
                {
                    var valueFromMessage = correlationProperty.ValueFromMessage(body);
                    var sagaData = await _sagaStorage.Find(sagaInvoker.Saga.GetSagaDataType(), correlationProperty.PropertyName, valueFromMessage);

                    if (sagaData == null) continue;

                    sagaInvoker.SetSagaData(sagaData);
                    foundExistingSagaData = true;
                    loadedSagaData.Add(new RelevantSagaInfo(sagaData, correlationProperties, sagaInvoker.Saga));

                    _log.Debug("Found existing saga data with ID {0} for message {1}", sagaData.Id, label);
                    break;
                }

                if (!foundExistingSagaData)
                {
                    var messageType = body.GetType();
                    var canBeInitiatedByThisMessageType = sagaInvoker.CanBeInitiatedBy(messageType);

                    if (canBeInitiatedByThisMessageType)
                    {
                        var newSagaData = _sagaHelper.CreateNewSagaData(sagaInvoker.Saga);
                        sagaInvoker.SetSagaData(newSagaData);
                        _log.Debug("Created new saga data with ID {0} for message {1}", newSagaData.Id, label);
                        newlyCreatedSagaData.Add(new RelevantSagaInfo(newSagaData, correlationProperties, sagaInvoker.Saga));
                    }
                    else
                    {
                        _log.Debug("Could not find existing saga data for message {0}", label);
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
            public RelevantSagaInfo(ISagaData sagaData, IEnumerable<CorrelationProperty> correlationProperties, Saga saga)
            {
                SagaData = sagaData;
                
                // only keep necessary correlation properties, i.e.
                CorrelationProperties = correlationProperties
                    .GroupBy(p => p.PropertyName)
                    .Select(g => g.First())
                    .ToList();

                Saga = saga;
            }

            public ISagaData SagaData { get; private set; }
            
            public List<CorrelationProperty> CorrelationProperties { get; private set; }
            
            public Saga Saga { get; private set; }
        }
    }
}