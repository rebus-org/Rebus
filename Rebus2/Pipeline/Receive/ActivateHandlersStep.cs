using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Rebus2.Activation;
using Rebus2.Extensions;
using Rebus2.Handlers;
using Rebus2.Messages;

namespace Rebus2.Pipeline.Receive
{
    public class ActivateHandlersStep : IIncomingStep
    {
        readonly ConcurrentDictionary<Type, MethodInfo> _dispatchMethods = new ConcurrentDictionary<Type, MethodInfo>();
        readonly IHandlerActivator _handlerActivator;

        public ActivateHandlersStep(IHandlerActivator handlerActivator)
        {
            _handlerActivator = handlerActivator;
        }

        public async Task Process(IncomingStepContext context, Func<Task> next)
        {
            var message = context.Load<Message>();
            var messageId = message.Headers.GetValue(Headers.MessageId);
            var body = message.Body;
            var messageType = body.GetType();
            var methodToInvoke = _dispatchMethods
                .GetOrAdd(messageType, type => GetDispatchMethod(messageType));

            var handlerInvokers = (Task<List<HandlerInvoker>>)methodToInvoke.Invoke(this, new[] { messageId, body });

            var invokers = await handlerInvokers;

            context.Save(invokers);

            await next();
        }

        // ReSharper disable once UnusedMember.Local
        async Task<List<HandlerInvoker>>  GetHandlerInvokers<TMessage>(string messageId, TMessage message)
        {
            var handlers = await _handlerActivator.GetHandlers(message);

            return handlers
                .Select(handler => new HandlerInvoker<TMessage>(messageId, async () =>
                {
                    await handler.Handle(message);
                }))
                .Cast<HandlerInvoker>()
                .ToList();
        }

        MethodInfo GetDispatchMethod(Type messageType)
        {
            return GetType()
                .GetMethod("GetHandlerInvokers", BindingFlags.NonPublic | BindingFlags.Instance)
                .MakeGenericMethod(messageType);
        }
    }

    public abstract class HandlerInvoker
    {
        public abstract Task Invoke();
    }

    public class HandlerInvoker<TMessage> : HandlerInvoker
    {
        readonly string _messageId;
        readonly Func<Task> _action;

        public HandlerInvoker(string messageId, Func<Task> action)
        {
            _messageId = messageId;
            _action = action;
        }

        public override async Task Invoke()
        {
            await _action();
        }
    }
}