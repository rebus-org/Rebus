using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Rebus2.Extensions;
using Rebus2.Logging;
using Rebus2.Messages;
using Rebus2.Sagas;

namespace Rebus2.Pipeline.Receive
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

            foreach (var sagaInvoker in handlerInvokersForSagas)
            {
                var foundExistingSagaData = false;

                foreach (var correlationProperty in _sagaHelper.GetCorrelationProperties(sagaInvoker, body))
                {
                    var sagaData = await _sagaStorage.Find(correlationProperty.PropertyName, correlationProperty.ValueFromMessage);

                    if (sagaData == null) continue;

                    sagaInvoker.SetSagaData(sagaData);
                    foundExistingSagaData = true;

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
                    }
                    else
                    {
                        _log.Debug("Could not find existing saga data for message {0}", messageId);
                        sagaInvoker.SkipInvocation();
                    }
                }

            }

            await next();
        }
    }
}