using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Ponder;
using Rebus.Logging;
using Rebus.Messages;
using Rebus.Shared;
using Rebus.Timeout;

namespace Rebus.Bus
{
    /// <summary>
    /// Implements stuff that must happen when handling one single message.
    /// </summary>
    class Dispatcher
    {
        static ILog log;

        readonly IActivateHandlers activateHandlers;

        readonly Dictionary<Type, MethodInfo> activatorMethods = new Dictionary<Type, MethodInfo>();
        readonly Dictionary<Type, MethodInfo> dispatcherMethods = new Dictionary<Type, MethodInfo>();
        readonly Dictionary<Type, string[]> fieldsToIndexForGivenSagaDataType = new Dictionary<Type, string[]>();
        readonly IInspectHandlerPipeline inspectHandlerPipeline;
        readonly IStoreSagaData storeSagaData;
        readonly IStoreSubscriptions storeSubscriptions;
        readonly Dictionary<Type, Type[]> typesToDispatchCache = new Dictionary<Type, Type[]>();
        readonly string sagaDataIdPropertyName;
        readonly TimeoutRequestHandler timeoutRequestHandler;
        readonly TimeoutReplyHandler timeoutReplyHandler;

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
                          IHandleDeferredMessage handleDeferredMessage,
                          IStoreTimeouts storeTimeouts)
        {
            this.storeSagaData = storeSagaData;
            this.activateHandlers = activateHandlers;
            this.storeSubscriptions = storeSubscriptions;
            this.inspectHandlerPipeline = inspectHandlerPipeline;
            sagaDataIdPropertyName = Reflect.Path<ISagaData>(s => s.Id);

            if (storeTimeouts != null)
            {
                timeoutRequestHandler = new TimeoutRequestHandler(storeTimeouts, handleDeferredMessage);
            }
            timeoutReplyHandler = new TimeoutReplyHandler(handleDeferredMessage);
        }

        public event Action<object, Saga> UncorrelatedMessage = delegate { };

        /// <summary>
        /// Main entry point of the dispatcher. Dispatches the given message, doing handler
        /// lookup etc. Any exceptions thrown will bubble up.
        /// </summary>
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
                    throw new UnhandledMessageException(message);
                }

                var sagaHandlers = distinctHandlersToExecute.Where(h => h is Saga).ToArray();

                if (sagaHandlers.Length > 1)
                {
                    throw new MultipleSagaHandlersFoundException(message, sagaHandlers.Select(h => h.GetType()).ToArray());
                }

                foreach (var handler in distinctHandlersToExecute)
                {
                    log.Debug("Dispatching {0} to {1}", message, handler);

                    var handlerType = handler.GetType();

                    foreach (var typeToDispatch in GetTypesToDispatchToThisHandler(typesToDispatch, handlerType))
                    {
                        GetDispatcherMethod(typeToDispatch).Invoke(this, new object[] { message, handler });

                        if (MessageContext.MessageDispatchAborted) break;
                    }

                    if (MessageContext.MessageDispatchAborted) break;
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
                return new[] {(IHandleMessages<T>) new SubscriptionMessageHandler(storeSubscriptions)};
            }

            if (typeof(T) == typeof(TimeoutRequest))
            {
                if (timeoutRequestHandler == null)
                {
                    throw new InvalidOperationException(string.Format(@"Received a TimeoutRequest, but there is not configured implementation of IStoreTimeouts in this Rebus endpoint.

This most likely indicates that you have configured this Rebus service to use an external timeout manager, but accidentally configured the timeout manager endpoint address to be the same as this endpoint's input queue."));
                }
                return new[] {(IHandleMessages<T>) timeoutRequestHandler};
            }

            if (typeof(T) == typeof(TimeoutReply))
            {
                return new[] {(IHandleMessages<T>) timeoutReplyHandler};
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
        ///   Private dispatcher method that gets invoked only via reflection.
        /// </summary>
        // ReSharper disable UnusedMember.Local
        void DispatchToHandler<TMessage>(TMessage message, IHandleMessages<TMessage> handler)
        {
            var saga = handler as Saga;
            if (saga != null)
            {
                saga.ConfigureHowToFindSaga();
                var sagaData = GetSagaData(message, saga);

                if (sagaData == null)
                {
                    if (handler is IAmInitiatedBy<TMessage>)
                    {
                        saga.IsNew = true;
                        saga.Complete = false;
                        sagaData = CreateSagaData(handler);
                    }
                    else
                    {
                        log.Warn("No saga data was found for {0}", handler);
                        UncorrelatedMessage(message, saga);
                        return;
                    }
                }
                else
                {
                    saga.IsNew = false;
                    saga.Complete = false;
                }

                handler.GetType().GetProperty("Data").SetValue(handler, sagaData, null);

                using (new SagaContext(sagaData.Id))
                {
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

            var sagaDataPropertyPathsToIndex = GetSagaDataPropertyPathsToIndex(saga).Distinct().ToArray();
            if (!saga.IsNew)
            {
                storeSagaData.Update(sagaData, sagaDataPropertyPathsToIndex);
            }
            else
            {
                storeSagaData.Insert(sagaData, sagaDataPropertyPathsToIndex);
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
            var sagaDataType = saga.GetType().GetProperty("Data").PropertyType;

            var correlations = saga.Correlations;

            // if correlation is set up, insist on using it
            if (correlations.ContainsKey(typeof(TMessage)))
            {
                var correlation = correlations[typeof(TMessage)];
                var fieldFromMessage = correlation.FieldFromMessage(message);
                var sagaDataPropertyPath = correlation.SagaDataPropertyPath;

                var sagaData = GetSagaData(sagaDataType, sagaDataPropertyPath, fieldFromMessage);

                return (ISagaData)sagaData;
            }

            // otherwise, see if we can do auto-correlation
            if (MessageContext.HasCurrent)
            {
                var messageContext = MessageContext.GetCurrent();

                // if the incoming message contains a saga auto-correlation id, try to load that specific saga
                if (messageContext.Headers.ContainsKey(Headers.AutoCorrelationSagaId))
                {
                    var sagaId = messageContext.Headers[Headers.AutoCorrelationSagaId].ToString();
                    var data = GetSagaData(sagaDataType, sagaDataIdPropertyName, sagaId);

                    // if we found the saga, return it - otherwise, fall back to correlating properties, if anything has been set up
                    if (data != null) return (ISagaData)data;
                }
            }

            // last option: bail out :)
            return null;
        }

        object GetSagaData(Type sagaDataType, string sagaDataPropertyPath, object fieldFromMessage)
        {
            var sagaData = storeSagaData.GetType()
                .GetMethod("Find").MakeGenericMethod(sagaDataType)
                .Invoke(storeSagaData, new[] { sagaDataPropertyPath, fieldFromMessage ?? "" });

            return sagaData;
        }
    }
}