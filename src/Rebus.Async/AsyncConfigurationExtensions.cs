using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Timers;
using Rebus.Bus;
using Rebus.Configuration;
using Rebus.Logging;
using Rebus.Shared;
using System.Linq;

namespace Rebus.Async
{
    /// <summary>
    /// Configuration extensions for configuring inline registration of inline reply handler callback
    /// </summary>
    public static class AsyncConfigurationExtensions
    {
        static internal ConcurrentDictionary<string, ReplyCallback> registeredReplyHandlers = new ConcurrentDictionary<string, ReplyCallback>();
        static readonly TimeSpan DefaultMaxCallbackAge = TimeSpan.FromHours(1);

        internal class ReplyCallback
        {
            public Delegate Callback { get; set; }
            public DateTime RegistrationTime { get; set; }
            public Action TimeoutCallback { get; set; }
            public TimeSpan MaxCallbackAge { get; set; }

            public void TimeOut()
            {
                if (TimeoutCallback == null) return;

                TimeoutCallback();
            }
        }

        /// <summary>
        /// Enables the ability to handle replies inline - i.e. when Rebus is configured with this option, you can go
        /// <code>
        /// bus.Send(new SomeRequest{...}, (SomeReply reply) => // do something);
        /// </code>
        /// and more.
        /// </summary>
        public static RebusConfigurer EnableInlineReplyHandlers(this RebusConfigurer configurer)
        {
            configurer.AddDecoration(b =>
                {
                    b.ActivateHandlers = new CorrelationHandlerInjector(b.ActivateHandlers);
                });
            return configurer;
        }

        /// <summary>
        /// Sends the specified <see cref="message"/> with an assigned correlation ID and stores the given 
        /// <see cref="replyHandler"/> to be invoked if/when a reply is returned. The default timeout of
        /// 1 hour applies, which causes the registered callback to be removed at that time in order to
        /// avoid leaking registered reply handlers.
        /// </summary>
        public static void Send<TReply>(this IBus bus, object message, Action<TReply> replyHandler)
        {
            InnerSend(bus, message, replyHandler, DefaultMaxCallbackAge);
        }

        /// <summary>
        /// Sends the specified <see cref="message"/> with an assigned correlation ID and stores the given 
        /// <see cref="replyHandler"/> to be invoked if/when a reply is returned. The timeout specified
        /// by <see cref="timeout"/> applies, which causes the registered callback to be removed at that time in order to
        /// avoid leaking registered reply handlers. When the timeout occurs, <see cref="timeoutAction"/>
        /// is invoked.
        /// </summary>
        public static void Send<TReply>(this IBus bus, object message, Action<TReply> replyHandler, TimeSpan timeout, Action timeoutAction)
        {
            InnerSend(bus, message, replyHandler, timeout, timeoutAction);
        }

        static void InnerSend<TReply>(IBus bus, object message, Action<TReply> replyHandler, TimeSpan timeout, Action timeoutAction = null)
        {
            if (bus == null) throw new ArgumentNullException("bus", "You cannot call these methods without a bus instance");
            if (message == null) throw new ArgumentNullException("message", "You cannot call these methods without a message");
            if (replyHandler == null) throw new ArgumentNullException("replyHandler", "Please specify a reply handler");

            var correlationId = Guid.NewGuid().ToString();
            bus.AttachHeader(message, Headers.CorrelationId, correlationId);

            var replyCallback = new ReplyCallback
                                    {
                                        Callback = replyHandler, 
                                        RegistrationTime = DateTime.UtcNow,
                                        MaxCallbackAge = timeout,
                                        TimeoutCallback = timeoutAction,
                                    };
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
            static ILog log;

            static CorrelationHandlerInjector()
            {
                RebusLoggerFactory.Changed += f => log = f.GetCurrentClassLogger();
            }

            readonly Timer cleanupTimer = new Timer();
            readonly IActivateHandlers innerActivator;

            public CorrelationHandlerInjector(IActivateHandlers innerActivator)
            {
                this.innerActivator = innerActivator;

                cleanupTimer.Elapsed += (o, ea) => DoCleanup();
                cleanupTimer.Interval = TimeSpan.FromSeconds(1).TotalMilliseconds;
                cleanupTimer.Start();
            }

            public IEnumerable<IHandleMessages> GetHandlerInstancesFor<T>()
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
                log.Info("Adding in-mem callback handler for {0}", replyCallback.Callback);
                handlerInstancesToReturn.Add(new ReplyMessageHandler<T>(replyCallback, correlationId));

                return handlerInstancesToReturn;
            }

            void DoCleanup()
            {
                var keys = registeredReplyHandlers.Keys.ToList();

                foreach (var key in keys)
                {
                    ReplyCallback replyHandler;
                    if (!registeredReplyHandlers.TryGetValue(key, out replyHandler)) continue;
                    var maxCallbackAgeForThisCallback = replyHandler.MaxCallbackAge;

                    if ((DateTime.UtcNow - replyHandler.RegistrationTime) < maxCallbackAgeForThisCallback) continue;

                    if (!registeredReplyHandlers.TryRemove(key, out replyHandler)) continue;

                    log.Warn("Registered callback {0} exceeded max age of {1}", replyHandler.Callback, maxCallbackAgeForThisCallback);
                    replyHandler.TimeOut();
                }
            }

            public void Release(IEnumerable handlerInstances)
            {
                innerActivator.Release(handlerInstances);
            }

            public void Dispose()
            {
                cleanupTimer.Stop();
                cleanupTimer.Dispose();

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
