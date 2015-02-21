using System;
using System.Collections.Concurrent;
using System.Reflection;
using System.Threading.Tasks;
using Rebus2.Activation;
using Rebus2.Extensions;
using Rebus2.Logging;
using Rebus2.Messages;

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

        public async Task Dispatch(Message message)
        {
            var messageId = message.Headers.GetValue(Headers.MessageId);
            var messageBody = message.Body;
            var messageType = messageBody.GetType();

            var methodToInvoke = _dispatchMethods
                .GetOrAdd(messageType, type => GetDispatchMethod(messageType));

            var dispatchAwaitable = (Task)methodToInvoke.Invoke(this, new[] { messageId, messageBody });

            await dispatchAwaitable;
        }

        // ReSharper disable once UnusedMember.Local
        async Task InnerDispatch<TMessage>(string messageId, TMessage message)
        {
            _log.Info("Dispatching message {0}", message);

            var messageWasHandled = false;

            foreach (var handler in await _handlerActivator.GetHandlers<TMessage>())
            {
                await handler.Handle(message);

                messageWasHandled = true;
            }

            if (!messageWasHandled)
            {
                throw new ApplicationException(string.Format("Message with ID {0} could not be handled by any handlers!", messageId));
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