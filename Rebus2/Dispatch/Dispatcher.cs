using System;
using System.Collections.Concurrent;
using System.Reflection;
using System.Threading.Tasks;
using Rebus2.Activation;
using Rebus2.Logging;

namespace Rebus2.Dispatch
{
    public class Dispatcher
    {
        static ILog _log;

        static Dispatcher()
        {
            RebusLoggerFactory.Changed += f => _log = f.GetCurrentClassLogger();
        }

        readonly ConcurrentDictionary<Type, MethodInfo> _dispatchMethods = new ConcurrentDictionary<Type, MethodInfo>();
        readonly IHandlerActivator _handlerActivator;

        public Dispatcher(IHandlerActivator handlerActivator)
        {
            _handlerActivator = handlerActivator;
        }

        public async Task Dispatch(object message)
        {
            var messageType = message.GetType();

            var methodToInvoke = _dispatchMethods
                .GetOrAdd(messageType, type => GetDispatchMethod(messageType));

            var dispatchAwaitable = (Task)methodToInvoke.Invoke(this, new[] { message });

            await dispatchAwaitable;
        }

        // ReSharper disable once UnusedMember.Local
        async Task InnerDispatch<TMessage>(TMessage message)
        {
            _log.Info("Dispatching message {0}", message);

            foreach (var handler in await _handlerActivator.GetHandlers<TMessage>())
            {
                await handler.Handle(message);
            }
        }

        MethodInfo GetDispatchMethod(Type messageType)
        {
            return GetType()
                .GetMethod("InnerDispatch", BindingFlags.NonPublic | BindingFlags.Instance)
                .MakeGenericMethod(messageType);
        }
    }
}