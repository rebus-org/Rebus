using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Rebus.Activation;
using Rebus.Extensions;
using Rebus.Messages;
using Rebus.Transport;
// ReSharper disable UnusedMethodReturnValue.Local

namespace Rebus.Pipeline.Receive
{
    /// <summary>
    /// Incoming message step that gets relevant handlers for the message
    /// </summary>
    [StepDocumentation(@"Looks at the incoming message and decides how to handle it. A HandlerInvokers object is saved to the context to be invoked later.")]
    public class ActivateHandlersStep : IIncomingStep
    {
        readonly ConcurrentDictionary<Type, MethodInfo> _dispatchMethods = new ConcurrentDictionary<Type, MethodInfo>();
        readonly IHandlerActivator _handlerActivator;

        /// <summary>
        /// Constructs the step with the <see cref="IHandlerActivator"/> to use to get the handler instances
        /// </summary>
        public ActivateHandlersStep(IHandlerActivator handlerActivator)
        {
            _handlerActivator = handlerActivator;
        }

        /// <summary>
        /// Looks up handlers for the incoming message and saves the handlers (without invoking them) to the context as a <see cref="HandlerInvokers"/>
        /// </summary>
        public async Task Process(IncomingStepContext context, Func<Task> next)
        {
            var transactionContext = context.Load<ITransactionContext>();
            var message = context.Load<Message>();
            var messageId = message.Headers.GetValue(Headers.MessageId);
            var body = message.Body;
            var messageType = body.GetType();
            var methodToInvoke = _dispatchMethods
                .GetOrAdd(messageType, type => GetDispatchMethod(messageType));

            var handlerInvokers = await (Task<HandlerInvokers>)methodToInvoke.Invoke(this, new[] { messageId, body, transactionContext, message });

            context.Save(handlerInvokers);

            await next();
        }

        async Task<HandlerInvokers> GetHandlerInvokers<TMessage>(string messageId, TMessage message, ITransactionContext transactionContext, Message logicalMessage)
        {
            var handlers = await _handlerActivator.GetHandlers(message, transactionContext);

            var listOfHandlerInvokers = handlers
                .Select(handler => new HandlerInvoker<TMessage>(messageId, () => handler.Handle(message), handler, transactionContext))
                .Cast<HandlerInvoker>()
                .ToList();

            return new HandlerInvokers(logicalMessage, listOfHandlerInvokers);
        }

        MethodInfo GetDispatchMethod(Type messageType)
        {
            return GetType().GetTypeInfo()
                .GetMethod(nameof(GetHandlerInvokers), BindingFlags.NonPublic | BindingFlags.Instance)
                .MakeGenericMethod(messageType);
        }
    }
}