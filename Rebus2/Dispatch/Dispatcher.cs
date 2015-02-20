using System;
using System.Collections.Concurrent;
using System.Reflection;
using System.Threading.Tasks;
using Rebus2.Activation;
using Rebus2.Logging;
using Rebus2.Serialization;

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

            _dispatchMethods
                .GetOrAdd(messageType, type => GetDispatchMethod(messageType))
                .Invoke(this, new[] {message});
        }

        MethodInfo GetDispatchMethod(Type messageType)
        {
            return GetType()
                .GetMethod("InnerDispatch", BindingFlags.NonPublic | BindingFlags.Instance)
                .MakeGenericMethod(messageType);
        }

        // ReSharper disable once UnusedMember.Local
        void InnerDispatch<TMessage>(TMessage message)
        {
            _log.Info("Dispatching message {0}", message);
        }
    }
}