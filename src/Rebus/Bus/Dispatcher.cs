using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Rebus.Messages;

namespace Rebus.Bus
{
    public class Dispatcher
    {
        readonly IStoreSagaData storeSagaData;
        readonly IActivateHandlers activateHandlers;
        readonly IStoreSubscriptions storeSubscriptions;

        public Dispatcher(IStoreSagaData storeSagaData, IActivateHandlers activateHandlers, IStoreSubscriptions storeSubscriptions)
        {
            this.storeSagaData = storeSagaData;
            this.activateHandlers = activateHandlers;
            this.storeSubscriptions = storeSubscriptions;
        }

        public void Dispatch<TMessage>(TMessage message)
        {
            IHandleMessages<TMessage>[] handlers = null;

            try
            {
                var handlerInstances = activateHandlers.GetHandlerInstancesFor<TMessage>();

                // if we didn't get anything, just carry on... might not be what we want, but let's just do that for now
                if (handlerInstances == null) return;

                handlers = handlerInstances.ToArray();

                foreach (var handler in handlers.Concat(OwnHandlersFor<TMessage>()))
                {
                    DispatchToHandlers(message, handler);
                }
            }
            finally
            {
                if (handlers != null)
                {
                    activateHandlers.ReleaseHandlerInstances(handlers);
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

        void DispatchToHandlers<TMessage>(TMessage message, IHandleMessages<TMessage> handler)
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
                saga.ConfigureHowToFindSaga();
                var sagaDataPropertyPathsToIndex = saga.Correlations.Values.Select(v => v.SagaDataPropertyPath).ToArray();
                storeSagaData.Save(sagaData, sagaDataPropertyPathsToIndex);
            }
            else
            {
                storeSagaData.Delete(sagaData);
            }
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

            return storeSagaData.Find(sagaDataPropertyPath, (fieldFromMessage ?? ""));
        }
    }
}