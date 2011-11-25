using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Rebus.Logging;
using Rebus.Messages;

namespace Rebus.Bus
{
    public class Dispatcher
    {
        static readonly ILog Log = RebusLoggerFactory.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        readonly ConcurrentDictionary<Type, Type[]> typesToDispatchCache = new ConcurrentDictionary<Type, Type[]>();
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
            IHandleMessages[] handlersToRelease = null;

            try
            {
                var typesToDispatch = GetTypesToDispatch(typeof (TMessage));
                var handlersFromActivator = typesToDispatch.SelectMany(type => GetHandlerInstancesFor(type));
                var handlerInstances = handlersFromActivator.ToArray();

                // add own internal handlers
                var handlerPipeline = handlerInstances.Concat(OwnHandlersFor<TMessage>()).ToList();

                // allow pipeline to be filtered
                var handlersToExecute = inspectHandlerPipeline.Filter(message, handlerPipeline).ToArray();

                // keep track of all handlers pulled from the activator as well as any handlers
                // that may have been added from the handler filter
                handlersToRelease = handlerInstances.Union(handlersToExecute).ToArray();

                foreach (var handler in handlersToExecute.Distinct())
                {
                    Log.Debug("Dispatching {0} to {1}", message, handler);

                    var handlerType = handler.GetType();

                    foreach(var typeToDispatch in GetTypesToDispatchToThisHandler(typesToDispatch, handlerType))
                    {
                        GetType().GetMethod("DispatchToHandler", BindingFlags.NonPublic | BindingFlags.Instance)
                            .MakeGenericMethod(typeToDispatch)
                            .Invoke(this, new object[] {message, handler});
                        
                        if (MessageContext.HasCurrent && !MessageContext.GetCurrent().DispatchMessageToHandlers) break;
                    }
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

        static Type[] GetTypesToDispatchToThisHandler(Type[] typesToDispatch, Type handlerType)
        {
            var interfaces = handlerType.GetInterfaces()
                .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof (IHandleMessages<>))
                .Select(i => i.GetGenericArguments()[0])
                .ToArray();

            return interfaces.Intersect(typesToDispatch).ToArray();
        }

        IEnumerable<IHandleMessages> GetHandlerInstancesFor(Type messageType)
        {
            var activationMethod = GeHandlerActivatorMethod(messageType);
            var handlerInstances = (IEnumerable<IHandleMessages>)activationMethod.Invoke(activateHandlers, new object[0]);
            return handlerInstances;
        }

        class HandlerInstance
        {
            public HandlerInstance(Type typeToDispatch, IHandleMessages handler)
            {
                TypeToDispatch = typeToDispatch;
                Handler = handler;
            }

            public Type TypeToDispatch { get; set; }
            public IHandleMessages Handler { get; set; }
        }

        MethodInfo GeHandlerActivatorMethod(Type messageType)
        {
            var methodInfo = activateHandlers.GetType().GetMethod("GetHandlerInstancesFor");
            var method = methodInfo.MakeGenericMethod(messageType);
            return method;
        }

        IEnumerable<IHandleMessages<T>> OwnHandlersFor<T>()
        {
            if (typeof(T) == typeof(SubscriptionMessage))
            {
                return new[] {(IHandleMessages<T>) new SubscriptionMessageHandler(storeSubscriptions)};
            }
            return new IHandleMessages<T>[0];
        }

        Type[] GetTypesToDispatch(Type messageType)
        {
            Type[] typesToDispatch;
            if (typesToDispatchCache.TryGetValue(messageType, out typesToDispatch))
            {
                return typesToDispatch;
            }
            var types = new HashSet<Type>();
            AddTypesFrom(messageType, types);
            var newArrayOfTypesToDispatch = types.ToArray();
            typesToDispatchCache.TryAdd(messageType, newArrayOfTypesToDispatch);
            return newArrayOfTypesToDispatch;
        }

        void AddTypesFrom(Type messageType, HashSet<Type> typeSet)
        {
            typeSet.Add(messageType);
            foreach (var interfaceType in messageType.GetInterfaces())
            {
                typeSet.Add(interfaceType);
            }
            if (messageType.BaseType != null)
            {
                AddTypesFrom(messageType.BaseType, typeSet);
            }
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