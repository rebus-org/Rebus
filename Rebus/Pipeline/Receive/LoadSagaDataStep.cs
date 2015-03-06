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
    public class LoadSagaDataStep : IIncomingStep
    {
        static ILog _log;

        static LoadSagaDataStep()
        {
            RebusLoggerFactory.Changed += f => _log = f.GetCurrentClassLogger();
        }

        readonly SagaHelper _sagaHelper = new SagaHelper();
        readonly ISagaStorage _sagaStorage;

        public LoadSagaDataStep(ISagaStorage sagaStorage)
        {
            _sagaStorage = sagaStorage;
        }

        public async Task Process(IncomingStepContext context, Func<Task> next)
        {
            var handlerInvokersForSagas = context.Load<List<HandlerInvoker>>()
                .Where(l => l.HasSaga)
                .ToList();

            var message = context.Load<Message>();
            var messageId = message.Headers.GetValue(Headers.MessageId);
            var body = message.Body;
            var loadedSagaData = new List<Tuple<ISagaData, List<CorrelationProperty>>>();
            var newlyCreatedSagaData = new List<Tuple<ISagaData, List<CorrelationProperty>>>();

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
                    loadedSagaData.Add(Tuple.Create(sagaData, correlationProperties));

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
                        newlyCreatedSagaData.Add(Tuple.Create(newSagaData, correlationProperties));
                    }
                    else
                    {
                        _log.Debug("Could not find existing saga data for message {0}", messageId);
                        sagaInvoker.SkipInvocation();
                    }
                }

            }

            await next();

            foreach (var sagaDataToInsert in newlyCreatedSagaData)
            {
                await _sagaStorage.Insert(sagaDataToInsert.Item1, sagaDataToInsert.Item2);
            }
            
            foreach (var sagaDataToUpdate in loadedSagaData)
            {
                await _sagaStorage.Update(sagaDataToUpdate.Item1, sagaDataToUpdate.Item2);
            }
        }
    }
}