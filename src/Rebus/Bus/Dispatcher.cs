using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Rebus.Extensions;
using Rebus.Logging;
using Rebus.Messages;

namespace Rebus.Bus
{
    /// <summary>
    ///   Implements stuff that must happen when handling one single message.
    /// </summary>
    class Dispatcher
    {
        private static ILog log;
        private readonly IActivateHandlers activateHandlers;

        private readonly Dictionary<Type, MethodInfo> activatorMethods = new Dictionary<Type, MethodInfo>();
        private readonly Dictionary<Type, MethodInfo> dispatcherMethods = new Dictionary<Type, MethodInfo>();
        private readonly Dictionary<Type, string[]> fieldsToIndexForGivenSagaDataType = new Dictionary<Type, string[]>();
        private readonly IInspectHandlerPipeline inspectHandlerPipeline;
        readonly IHandleDeferredMessage handleDeferredMessage;

        private readonly IStoreSagaData storeSagaData;
        private readonly IStoreSubscriptions storeSubscriptions;
        private readonly Dictionary<Type, Type[]> typesToDispatchCache = new Dictionary<Type, Type[]>();

        static Dispatcher()
        {
            RebusLoggerFactory.Changed += f => log = f.GetCurrentClassLogger();
        }

        /// <summary>
        /// Constructs the dispatcher with the specified instances to store and retrieve saga data,
        /// create message handlers, store and retrieve subscriptions, and to inspect and
        /// possibly rearrange the handler pipeline.
        /// </summary>
        public Dispatcher(IStoreSagaData storeSagaData,
                          IActivateHandlers activateHandlers,
                          IStoreSubscriptions storeSubscriptions,
                          IInspectHandlerPipeline inspectHandlerPipeline,
                          IHandleDeferredMessage handleDeferredMessage)
        {
            this.storeSagaData = storeSagaData;
            this.activateHandlers = activateHandlers;
            this.storeSubscriptions = storeSubscriptions;
            this.inspectHandlerPipeline = inspectHandlerPipeline;
            this.handleDeferredMessage = handleDeferredMessage;
        }

        /// <summary>
        /// Main entry point of the dispatcher. Dispatches the given message, doing handler
        /// lookup etc. Any exceptions thrown will bubble up.
        /// </summary>
        public void Dispatch<TMessage>(TMessage message)
        {
            IHandleMessages[] handlersToRelease = null;

            try
            {
                var typesToDispatch = GetTypesToDispatch(typeof (TMessage));
                var handlersFromActivator = typesToDispatch.SelectMany(GetHandlerInstances);
                var handlerInstances = handlersFromActivator.ToArray();

                // add own internal handlers
                var handlerPipeline = handlerInstances.Concat(OwnHandlersFor<TMessage>()).ToList();

                // allow pipeline to be filtered
                var handlersToExecute = inspectHandlerPipeline.Filter(message, handlerPipeline).ToArray();

                // keep track of all handlers pulled from the activator as well as any handlers
                // that may have been added from the handler filter
                handlersToRelease = handlerInstances.Union(handlersToExecute).ToArray();

                var distinctHandlersToExecute = handlersToExecute.Distinct().ToArray();

                if (!distinctHandlersToExecute.Any())
                {
                    log.Warn("The dispatcher could not find any handlers to execute with message of type {0}", typeof (TMessage));
                }
                else
                {
                    foreach (var handler in distinctHandlersToExecute)
                    {
                        log.Debug("Dispatching {0} to {1}", message, handler);

                        var handlerType = handler.GetType();

                        foreach (var typeToDispatch in GetTypesToDispatchToThisHandler(typesToDispatch, handlerType))
                        {
                            GetDispatcherMethod(typeToDispatch).Invoke(this, new object[] {message, handler});

                            if (MessageContext.MessageDispatchAborted) break;
                        }

                        if (MessageContext.MessageDispatchAborted) break;
                    }
                }
            }
            finally
            {
                if (handlersToRelease != null)
                {
                    try
                    {
                        activateHandlers.Release(handlersToRelease);
                    }
                    catch (Exception e)
                    {
                        log.Error(e, "An error occurred while attempting to release handlers: {0}", string.Join(", ", handlersToRelease.Select(h => h.GetType())));
                    }
                }
            }
        }

        private Type[] GetTypesToDispatch(Type messageType)
        {
            Type[] typesToDispatch;
            if (typesToDispatchCache.TryGetValue(messageType, out typesToDispatch))
            {
                return typesToDispatch;
            }

            var types = new HashSet<Type>();
            AddTypesFrom(messageType, types);
            var newArrayOfTypesToDispatch = types.ToArray();
            typesToDispatchCache[messageType] = newArrayOfTypesToDispatch;

            return newArrayOfTypesToDispatch;
        }

        private IEnumerable<IHandleMessages> GetHandlerInstances(Type messageType)
        {
            var activationMethod = GetActivationMethod(messageType);
            var handlers = activationMethod.Invoke(activateHandlers, new object[0]);
            var handlerInstances = (IEnumerable<IHandleMessages>) (handlers ?? new IHandleMessages[0]);
            return handlerInstances;
        }

        private MethodInfo GetActivationMethod(Type messageType)
        {
            MethodInfo method;
            if (activatorMethods.TryGetValue(messageType, out method)) return method;

            method = activateHandlers.GetType()
                .GetMethod("GetHandlerInstancesFor")
                .MakeGenericMethod(messageType);

            activatorMethods[messageType] = method;
            return method;
        }

        private MethodInfo GetDispatcherMethod(Type typeToDispatch)
        {
            MethodInfo method;
            if (dispatcherMethods.TryGetValue(typeToDispatch, out method)) return method;

            method = GetType()
                .GetMethod("DispatchToHandler", BindingFlags.NonPublic | BindingFlags.Instance)
                .MakeGenericMethod(typeToDispatch);

            dispatcherMethods[typeToDispatch] = method;
            return method;
        }

        private IEnumerable<Type> GetTypesToDispatchToThisHandler(IEnumerable<Type> typesToDispatch, Type handlerType)
        {
            var interfaces = handlerType.GetInterfaces()
                .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof (IHandleMessages<>))
                .Select(i => i.GetGenericArguments()[0]);

            return interfaces.Intersect(typesToDispatch).ToArray();
        }

        private IEnumerable<IHandleMessages<T>> OwnHandlersFor<T>()
        {
            if (typeof (T) == typeof (SubscriptionMessage))
            {
                return new[] {(IHandleMessages<T>) new SubscriptionMessageHandler(storeSubscriptions)};
            }
            
            if (typeof(T) == typeof(TimeoutReply))
            {
                return new[] {(IHandleMessages<T>) new TimeoutReplyHandler(handleDeferredMessage)};
            }
            
            return new IHandleMessages<T>[0];
        }

        private void AddTypesFrom(Type messageType, HashSet<Type> typeSet)
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

        /// <summary>
        ///   Private dispatcher method that gets invoked only via reflection.
        /// </summary>
        // ReSharper disable UnusedMember.Local
        private void DispatchToHandler<TMessage>(TMessage message, IHandleMessages<TMessage> handler)
        {
            var saga = handler as Saga;
            if (saga != null)
            {
                saga.ConfigureHowToFindSaga();
                var sagaDatas = GetSagaData(message, saga).ToList();

                saga.IsNew = !sagaDatas.Any();
                if (saga.IsNew)
                {
                    if (handler is IAmInitiatedBy<TMessage>)
                    {
                        sagaDatas = CreateSagaData(handler).AsEnumerable().ToList();
                    }
                    else
                    {
                        log.Warn("No saga data was found for {0}", handler);
                        return;
                    }
                }

                foreach (var sagaData in sagaDatas)
                {
                    handler.GetType().GetProperty("Data").SetValue(handler, sagaData, null);
                    handler.Handle(message);
                    PerformSaveActions(saga, sagaData);
                }
                return;
            }

            handler.Handle(message);
        }
        // ReSharper restore UnusedMember.Local

        void PerformSaveActions(Saga saga, ISagaData sagaData)
        {
            if (saga.Complete)
            {
                if (!saga.IsNew)
                {
                    storeSagaData.Delete(sagaData);
                }
                return;
            }

            var sagaDataPropertyPathsToIndex = GetSagaDataPropertyPathsToIndex(saga);

            if (!saga.IsNew)
            {
                storeSagaData.Update(sagaData, sagaDataPropertyPathsToIndex);
            }
            else
            {
                storeSagaData.Insert(sagaData, sagaDataPropertyPathsToIndex);
            }
        }

        private string[] GetSagaDataPropertyPathsToIndex(Saga saga)
        {
            string[] paths;
            var sagaType = saga.GetType();

            if (fieldsToIndexForGivenSagaDataType.TryGetValue(sagaType, out paths))
            {
                // yay! GO!
                return paths;
            }

            paths = saga.Correlations.Values.Select(v => v.SagaDataPropertyPath).ToArray();

            // make sure they're there the next time
            fieldsToIndexForGivenSagaDataType[sagaType] = paths;

            return paths;
        }

        private ISagaData CreateSagaData<TMessage>(IHandleMessages<TMessage> handler)
        {
            var dataProperty = handler.GetType().GetProperty("Data");
            var sagaData = (ISagaData) Activator.CreateInstance(dataProperty.PropertyType);
            sagaData.Id = Guid.NewGuid();
            return sagaData;
        }

        private IEnumerable<ISagaData> GetSagaData<TMessage>(TMessage message, Saga saga)
        {
            var correlations = saga.Correlations;

            if (!correlations.ContainsKey(typeof (TMessage))) return Enumerable.Empty<ISagaData>();

            var correlation = correlations[typeof (TMessage)];
            var fieldFromMessage = correlation.FieldFromMessage(message);
            var sagaDataPropertyPath = correlation.SagaDataPropertyPath;
            var sagaDataType = saga.GetType().GetProperty("Data").PropertyType;
            
            var sagaData = storeSagaData.GetType()
                .GetMethod("Find").MakeGenericMethod(sagaDataType)
                .Invoke(storeSagaData, new[] {sagaDataPropertyPath, fieldFromMessage ?? ""});

            return (IEnumerable<ISagaData>)sagaData;
        }
    }
}