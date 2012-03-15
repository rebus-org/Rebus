using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Rebus.Logging;
using Rebus.Messages;

namespace Rebus.Bus
{
    /// <summary>
    /// Implements stuff that must happen when handling one single message.
    /// </summary>
    public class Dispatcher
    {
        static ILog Log;

        static Dispatcher()
        {
            RebusLoggerFactory.Changed += f => Log = f.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        }

        readonly Dictionary<Type, MethodInfo> dispatcherMethods = new Dictionary<Type, MethodInfo>();
        readonly Dictionary<Type, MethodInfo> activatorMethods = new Dictionary<Type, MethodInfo>();
        readonly Dictionary<Type, Type[]> typesToDispatchCache = new Dictionary<Type, Type[]>();
        readonly Dictionary<Type, string[]> fieldsToIndexForGivenSagaDataType = new Dictionary<Type, string[]>();

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
                var typesToDispatch = GetTypesToDispatch(typeof(TMessage));
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
                    Log.Warn("The dispatcher could not find any handlers to execute with message of type {0}", typeof(TMessage));
                }
                else
                {
                    foreach (var handler in distinctHandlersToExecute)
                    {
                        Log.Debug("Dispatching {0} to {1}", message, handler);

                        var handlerType = handler.GetType();

                        foreach (var typeToDispatch in GetTypesToDispatchToThisHandler(typesToDispatch, handlerType))
                        {
                            GetDispatcherMethod(typeToDispatch).Invoke(this, new object[] { message, handler });

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
                        Log.Error(e, "An error occurred while attempting to release handlers: {0}", string.Join(", ", handlersToRelease.Select(h => h.GetType())));
                    }
                }
            }
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
            typesToDispatchCache[messageType] = newArrayOfTypesToDispatch;

            return newArrayOfTypesToDispatch;
        }

        IEnumerable<IHandleMessages> GetHandlerInstances(Type messageType)
        {
            var activationMethod = GetActivationMethod(messageType);
            var handlers = activationMethod.Invoke(activateHandlers, new object[0]);
            var handlerInstances = (IEnumerable<IHandleMessages>)(handlers ?? new IHandleMessages[0]);
            return handlerInstances;
        }

        MethodInfo GetActivationMethod(Type messageType)
        {
            MethodInfo method;
            if (activatorMethods.TryGetValue(messageType, out method)) return method;

            method = activateHandlers.GetType()
                .GetMethod("GetHandlerInstancesFor")
                .MakeGenericMethod(messageType);

            activatorMethods[messageType] = method;
            return method;
        }

        MethodInfo GetDispatcherMethod(Type typeToDispatch)
        {
            MethodInfo method;
            if (dispatcherMethods.TryGetValue(typeToDispatch, out method)) return method;

            method = GetType()
                .GetMethod("DispatchToHandler", BindingFlags.NonPublic | BindingFlags.Instance)
                .MakeGenericMethod(typeToDispatch);

            dispatcherMethods[typeToDispatch] = method;
            return method;
        }

        IEnumerable<Type> GetTypesToDispatchToThisHandler(IEnumerable<Type> typesToDispatch, Type handlerType)
        {
            var interfaces = handlerType.GetInterfaces()
                .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IHandleMessages<>))
                .Select(i => i.GetGenericArguments()[0]);

            return interfaces.Intersect(typesToDispatch).ToArray();
        }

        IEnumerable<IHandleMessages<T>> OwnHandlersFor<T>()
        {
            if (typeof(T) == typeof(SubscriptionMessage))
            {
                return new[] { (IHandleMessages<T>)new SubscriptionMessageHandler(storeSubscriptions) };
            }
            return new IHandleMessages<T>[0];
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

        /// <summary>
        /// Private dispatcher method that gets invoked only via reflection.
        /// </summary>
        // ReSharper disable UnusedMember.Local
        void DispatchToHandler<TMessage>(TMessage message, IHandleMessages<TMessage> handler)
        {
            var saga = handler as Saga;
            
            if (saga != null)
            {
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
        // ReSharper restore UnusedMember.Local

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
            fieldsToIndexForGivenSagaDataType[sagaType] = paths;

            return paths;
        }

        ISagaData CreateSagaData<TMessage>(IHandleMessages<TMessage> handler)
        {
            var dataProperty = handler.GetType().GetProperty("Data");
            var sagaData = (ISagaData)Activator.CreateInstance(dataProperty.PropertyType);
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