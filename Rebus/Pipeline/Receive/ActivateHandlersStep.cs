using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Rebus.Activation;
using Rebus.Extensions;
using Rebus.Messages;
using Rebus.Sagas;

namespace Rebus.Pipeline.Receive
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
        async Task<List<HandlerInvoker>> GetHandlerInvokers<TMessage>(string messageId, TMessage message)
        {
            var handlers = await _handlerActivator.GetHandlers(message);

            return handlers
                .Select(handler => new HandlerInvoker<TMessage>(messageId, () => handler.Handle(message), handler))
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
        public abstract bool HasSaga { get; }
        public abstract Saga Saga { get; }

        public abstract void SetSagaData(ISagaData sagaData);
        public abstract bool CanBeInitiatedBy(Type messageType);

        public abstract void SkipInvocation();
    }

    public class HandlerInvoker<TMessage> : HandlerInvoker
    {
        readonly string _messageId;
        readonly Func<Task> _action;
        readonly object _handler;
        ISagaData _sagaData;
        bool _invokeHandler = true;

        public HandlerInvoker(string messageId, Func<Task> action, object handler)
        {
            _messageId = messageId;
            _action = action;
            _handler = handler;
        }

        public override bool HasSaga
        {
            get { return _handler is Saga; }
        }

        public override Saga Saga
        {
            get
            {
                if (!HasSaga) throw new InvalidOperationException(string.Format("Attempted to get {0} as saga, it's not a saga!", _handler));
                
                return (Saga)_handler;
            }
        }

        public override async Task Invoke()
        {
            if (!_invokeHandler) return;

            await _action();
        }

        public override void SetSagaData(ISagaData sagaData)
        {
            if (!HasSaga) throw new InvalidOperationException(string.Format("Attempted to set {0} as saga data on handler {1}, but the handler is not a saga!",
                sagaData, _handler));

            var dataProperty = _handler.GetType().GetProperty("Data");

            if (dataProperty == null)
            {
                throw new ApplicationException(string.Format("Could not find the 'Data' property on {0}...", _handler));
            }

            dataProperty.SetValue(_handler, sagaData);

            _sagaData = sagaData;
        }

        public override bool CanBeInitiatedBy(Type messageType)
        {
            var handlerTypeToLookFor = typeof(IAmInitiatedBy<>).MakeGenericType(messageType);

            return _handler.GetType().GetInterfaces().Contains(handlerTypeToLookFor);
        }

        public override void SkipInvocation()
        {
            _invokeHandler = false;
        }
    }
}