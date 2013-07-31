using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using Rebus.Bus;
using Rebus.Configuration;
using Rebus.Shared;
using System.Linq;

namespace Rebus.WebAsync
{
    public static class ConfigurationEx
    {
        static internal ConcurrentDictionary<string, ReplyCallback> registeredReplyHandlers = new ConcurrentDictionary<string, ReplyCallback>();

        internal class ReplyCallback
        {
            public Delegate Callback { get; set; }
            public DateTime RegistrationTime { get; set; }
        }

        public static RebusConfigurer EnableWebCallbacks(this RebusConfigurer configurer)
        {
            configurer.AddDecoration(b =>
                {
                    b.ActivateHandlers = new CorrelationHandlerInjector(b.ActivateHandlers);
                });
            return configurer;
        }

        public static void Send<TReply>(this IBus bus, object message, Action<TReply> replyHandler)
        {
            var correlationId = Guid.NewGuid().ToString();
            bus.AttachHeader(message, Headers.CorrelationId, correlationId);
            
            var replyCallback = new ReplyCallback {Callback = replyHandler, RegistrationTime = DateTime.UtcNow};
            var key = GetKey(correlationId, typeof (TReply));

            if (TransactionContext.Current != null)
            {
                TransactionContext.Current.DoCommit +=
                    () => registeredReplyHandlers.TryAdd(key, replyCallback);
            }
            else
            {
                registeredReplyHandlers.TryAdd(key, replyCallback);
            }

            bus.Send(message);
        }

        static string GetKey(string correlationId, Type replyType)
        {
            return string.Format("{0} : {1}", replyType.FullName, correlationId);
        }

        class CorrelationHandlerInjector : IActivateHandlers, IDisposable
        {
            static readonly TimeSpan MaxCallbackAge = TimeSpan.FromHours(1);
            readonly IActivateHandlers innerActivator;
            int callCounter;

            public CorrelationHandlerInjector(IActivateHandlers innerActivator)
            {
                this.innerActivator = innerActivator;
            }

            public IEnumerable<IHandleMessages<T>> GetHandlerInstancesFor<T>()
            {
                var handlerInstancesToReturn = innerActivator
                    .GetHandlerInstancesFor<T>()
                    .ToList();

                // if there's no correlation ID in the header, just return
                var currentHeaders = MessageContext.GetCurrent().Headers;
                if (!currentHeaders.ContainsKey(Headers.CorrelationId))
                {
                    return handlerInstancesToReturn;
                }

                // if there's no registered callback for this correlationID/type, just return
                var correlationId = currentHeaders[Headers.CorrelationId].ToString();
                var key = GetKey(correlationId, typeof (T));
                if (!registeredReplyHandlers.ContainsKey(key))
                {
                    return handlerInstancesToReturn;
                }

                // if we don't succeed in retrieving the callback (unlikely!), just return
                ReplyCallback replyCallback;
                if (!registeredReplyHandlers.TryRemove(key, out replyCallback))
                {
                    return handlerInstancesToReturn;
                }

                // otherwise, we may add our special callback handler
                handlerInstancesToReturn.Add(new ReplyMessageHandler<T>(replyCallback, correlationId));

                PossiblyTriggerCleanup();

                return handlerInstancesToReturn;
            }

            void PossiblyTriggerCleanup()
            {
                var currentValue = Interlocked.Increment(ref callCounter);

                if (currentValue%100 != 0) return;

                var keys = registeredReplyHandlers.Keys.ToList();

                foreach (var key in keys)
                {
                    ReplyCallback replyHandler;
                    if (!registeredReplyHandlers.TryGetValue(key, out replyHandler)) continue;
                    if ((DateTime.UtcNow - replyHandler.RegistrationTime) < MaxCallbackAge) continue;
                    registeredReplyHandlers.TryRemove(key, out replyHandler);
                }
            }

            public void Release(IEnumerable handlerInstances)
            {
                innerActivator.Release(handlerInstances);
            }

            public void Dispose()
            {
                var activator = innerActivator as IDisposable;
                if (activator == null) return;
                
                activator.Dispose();
            }
        }

        internal class ReplyMessageHandler<TMessage> : IHandleMessages<TMessage>
        {
            readonly ReplyCallback replyCallbackToUse;
            readonly string correlationId;

            public ReplyMessageHandler(ReplyCallback replyCallbackToUse, string correlationId)
            {
                this.replyCallbackToUse = replyCallbackToUse;
                this.correlationId = correlationId;
            }

            public void Handle(TMessage message)
            {
                var callback = replyCallbackToUse.Callback;
                try
                {
                    callback.DynamicInvoke(message);
                }
                catch (Exception e)
                {
                    throw new ApplicationException(string.Format("An error occurred while attempting to dispatch received reply {0} with correlation ID {1} to callback {2}",
                        message, correlationId, callback), e);
                }
            }
        }
    }
}
