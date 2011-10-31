using System;
using System.Collections.Generic;
using System.Linq;
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
            if (handler is ISaga)
            {
                var saga = (ISaga)handler;
                saga.ConfigureHowToFindSaga();

                var dataProperty = handler.GetType().GetProperty("Data");
                ISagaData sagaData = GetSagaData(message, saga);

                if (sagaData == null)
                {
                    if (handler is IAmInitiatedBy<TMessage>)
                    {
                        sagaData = CreateSagaData(handler);

                        dataProperty.SetValue(handler, sagaData, new object[0]);
                        handler.Handle(message);
                        if (!saga.Complete)
                        {
                            SaveSageData(sagaData);
                        }
                        else
                        {
                            DeleteSagaData(sagaData);
                        }
                    }
                    return;
                }

                dataProperty.SetValue(handler, sagaData, new object[0]);
                handler.Handle(message);
                if (!saga.Complete)
                {
                    SaveSageData(sagaData);
                }
                else
                {
                    DeleteSagaData(sagaData);
                }

                return;
            }
            
            handler.Handle(message);
        }

        ISagaData CreateSagaData<TMessage>(IHandleMessages<TMessage> handler)
        {
            var dataProperty = handler.GetType().GetProperty("Data");

            return (ISagaData) Activator.CreateInstance(dataProperty.PropertyType);
        }

        void SaveSageData(ISagaData sagaData)
        {
            storeSagaData.Save(sagaData);
        }

        ISagaData GetSagaData<TMessage>(TMessage message, ISaga saga)
        {
            var correlations = saga.Correlations;

            if (!correlations.ContainsKey(typeof(TMessage))) return null;

            var correlation = (Correlation<TMessage>)correlations[typeof(TMessage)];
            var fieldFromMessage = correlation.MessageProperty(message);
            var sagaDataPropertyPath = correlation.SagaDataPropertyPath;

            return storeSagaData.Find(sagaDataPropertyPath, (fieldFromMessage ?? "").ToString());
        }

        void DeleteSagaData(ISagaData sagaData)
        {
            storeSagaData.Delete(sagaData);
        }
    }
}