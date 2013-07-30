using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Rebus.Configuration;
using Rebus.Shared;
using System.Linq;

namespace Rebus.WebAsync
{
    public static class ConfigurationEx
    {
        static internal ConcurrentDictionary<string, Delegate> registeredReplyHandlers = new ConcurrentDictionary<string, Delegate>();

        public static RebusConfigurer AllowWebCallbacks(this RebusConfigurer configurer)
        {
            configurer.AddDecoration(b =>
                {
                    b.ActivateHandlers = new CorrelationHandlerInjector(b.ActivateHandlers);
                });
            return configurer;
        }

        class CorrelationHandlerInjector : IActivateHandlers, IDisposable
        {
            readonly IActivateHandlers innerActivator;

            public CorrelationHandlerInjector(IActivateHandlers innerActivator)
            {
                this.innerActivator = innerActivator;
            }

            public IEnumerable<IHandleMessages<T>> GetHandlerInstancesFor<T>()
            {
                return innerActivator.GetHandlerInstancesFor<T>()
                                     .Concat(new[] {new ReplyMessageHandler<T>(registeredReplyHandlers)});
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

        public static void Send<TReply>(this IBus bus, object message, Action<TReply> replyHandler)
        {
            var correlationId = Guid.NewGuid().ToString();
            bus.AttachHeader(message, Headers.CorrelationId, correlationId);
            registeredReplyHandlers.TryAdd(correlationId, replyHandler);
            bus.Send(message);
        }
    }

    public class ReplyMessageHandler<T> : IHandleMessages<T>
    {
        readonly ConcurrentDictionary<string, Delegate> registeredReplyHandlers;

        public ReplyMessageHandler(ConcurrentDictionary<string, Delegate> registeredReplyHandlers)
        {
            this.registeredReplyHandlers = registeredReplyHandlers;
        }

        public void Handle(T message)
        {
            var currentHeaders = MessageContext.GetCurrent().Headers;
            if (!currentHeaders.ContainsKey(Headers.CorrelationId)) return;

            var correlationId = currentHeaders[Headers.CorrelationId].ToString();
            
            Delegate callback;
            if (!registeredReplyHandlers.TryRemove(correlationId, out callback))
            {
                return;
            }

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
