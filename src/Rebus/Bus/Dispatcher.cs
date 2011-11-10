using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Rebus.Messages;

namespace Rebus.Bus
{
    public class Dispatcher
    {
        readonly ConcurrentDictionary<Type, string[]> fieldsToIndexForGivenSagaDataType = new ConcurrentDictionary<Type, string[]>();

        readonly IStoreSagaData storeSagaData;
        readonly IActivateHandlers activateHandlers;
        readonly IStoreSubscriptions storeSubscriptions;
        readonly IInspectHandlerPipeline inspectHandlerPipeline;

        public Dispatcher(IStoreSagaData storeSagaData, 
            IActivateHandlers activateHandlers, 
            IStoreSubscriptions storeSubscriptions,
            IInspectHandlerPipeline inspectHandlerPipeline)
        {
            this.storeSagaData = storeSagaData;
            this.activateHandlers = activateHandlers;
            this.storeSubscriptions = storeSubscriptions;
            this.inspectHandlerPipeline = inspectHandlerPipeline;
        }

        public void Dispatch<TMessage>(TMessage message)
        {
            IHandleMessages<TMessage>[] handlersToRelease = null;

            try
            {
                var handlerInstances = activateHandlers.GetHandlerInstancesFor<TMessage>();

                // if we didn't get anything, just carry on... might not be what we want, but let's just do that for now
                if (handlerInstances == null) return;

                // evaluate handler sequence and ensure that its "fixed in place"
                handlersToRelease = handlerInstances.Concat(OwnHandlersFor<TMessage>()).ToArray();

                // allow pipeline to be filtered
                var handlersToExecute = inspectHandlerPipeline.Filter(message, handlersToRelease).ToList();

                // keep track of all handlers pulled from the activator as well as any handlers
                // that may have been added from the handler filter
                handlersToRelease = handlersToRelease.Union(handlersToExecute).ToArray();

                foreach (var handler in handlersToExecute)
                {
                    DispatchToHandler(message, handler);
                    if (MessageContext.HasCurrent && !MessageContext.GetCurrent().DispatchMessageToHandlers) break;
                }
            }
            finally
            {
                if (handlersToRelease != null)
                {
                    activateHandlers.ReleaseHandlerInstances(handlersToRelease);
                }
            }
        }

        IEnumerable<IHandleMessages<T>> OwnHandlersFor<T>()
        {
            if (typeof(T) == typeof(SubscriptionMessage))
            {
                return new[] { (IHandleMessages<T>)new SubscriptionMessageHandler(storeSubscriptions) };
            }

            return new IHandleMessages<T>[0];
        }

        void DispatchToHandler<TMessage>(TMessage message, IHandleMessages<TMessage> handler)
        {
            if (handler is Saga)
            {
                var saga = (Saga)handler;

                var dataProperty = handler.GetType().GetProperty("Data");
                var sagaData = GetSagaData(message, saga);

                if (sagaData == null)
                {
                    if (handler is IAmInitiatedBy<TMessage>)
                    {
                        sagaData = CreateSagaData(handler);
                        PerformSaveActions(message, handler, saga, dataProperty, sagaData);
                    }
                    return;
                }
                PerformSaveActions(message, handler, saga, dataProperty, sagaData);
                return;
            }
            
            handler.Handle(message);
        }

        void PerformSaveActions<TMessage>(TMessage message, IHandleMessages<TMessage> handler, Saga saga, PropertyInfo dataProperty, ISagaData sagaData)
        {
            dataProperty.SetValue(handler, sagaData, new object[0]);
            handler.Handle(message);
            
            if (!saga.Complete)
            {
                var sagaDataPropertyPathsToIndex = GetSagaDataPropertyPathsToIndex(saga);
                storeSagaData.Save(sagaData, sagaDataPropertyPathsToIndex);
            }
            else
            {
                storeSagaData.Delete(sagaData);
            }
        }

        string[] GetSagaDataPropertyPathsToIndex(Saga saga)
        {
            string[] paths;
            var sagaType = saga.GetType();
            
            if (fieldsToIndexForGivenSagaDataType.TryGetValue(sagaType, out paths))
            {
                // yay! GO!
                return paths;
            }

            // sigh! we have to ask the saga to generate its correlations for us...
            saga.ConfigureHowToFindSaga();
            paths = saga.Correlations.Values.Select(v => v.SagaDataPropertyPath).ToArray();

            // make sure they're there the next time
            fieldsToIndexForGivenSagaDataType.TryAdd(sagaType, paths);

            return paths;
        }

        ISagaData CreateSagaData<TMessage>(IHandleMessages<TMessage> handler)
        {
            var dataProperty = handler.GetType().GetProperty("Data");
            var sagaData = (ISagaData) Activator.CreateInstance(dataProperty.PropertyType);
            sagaData.Id = Guid.NewGuid();
            return sagaData;
        }

        ISagaData GetSagaData<TMessage>(TMessage message, Saga saga)
        {
            var correlations = saga.Correlations;

            if (!correlations.ContainsKey(typeof(TMessage))) return null;

            var correlation = correlations[typeof(TMessage)];
            var fieldFromMessage = correlation.FieldFromMessage(message);
            var sagaDataPropertyPath = correlation.SagaDataPropertyPath;

            return storeSagaData.Find(sagaDataPropertyPath, (fieldFromMessage ?? ""),
                                      saga.GetType().GetProperty("Data").PropertyType);
        }
    }
}